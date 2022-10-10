#r "nuget: Suave, 2.6.2"

open System
open System.IO
open System.Net
open Suave
open Suave.Operators
open Suave.Filters
open Suave.RequestErrors
open Suave.Sockets
open Suave.Writers
open Suave.Sockets.Control

let sendStream (createStream : unit -> Stream) (compression : bool) : WebPart =
  fun (ctx : HttpContext) ->

    let writeStream () =
      let stream = createStream()

      let start = 0L
      let total = stream.Length
      let status = HTTP_200.status

      fun (conn, _) -> socket {
        let lastModified = fun _ -> DateTime.UtcNow
        let key = Guid.NewGuid() |> string

        let! (encoding,fs) = Compression.transformStream key stream lastModified compression ctx.runtime.compressionFolder ctx
        let finish = start + fs.Length - 1L
        try
          match encoding with
          | Some n ->
            let! (_,conn) = asyncWriteLn ("Content-Range: bytes " + start.ToString() + "-" + finish.ToString() + "/*") conn
            let! (_,conn) = asyncWriteLn (String.Concat [| "Content-Encoding: "; n.ToString() |]) conn
            let! (_,conn) = asyncWriteLn ("Content-Length: " + stream.Length.ToString() + "\r\n") conn
            let! conn = flush conn
            if ctx.request.``method`` <> HttpMethod.HEAD && fs.Length > 0L then
              do! transferStream conn fs
            return conn
          | None ->
            let! (_,conn) = asyncWriteLn ("Content-Range: bytes " + start.ToString() + "-" + finish.ToString() + "/" + total.ToString()) conn
            let! (_,conn) = asyncWriteLn ("Content-Length: " + stream.Length.ToString() + "\r\n") conn
            let! conn = flush conn
            if ctx.request.``method`` <> HttpMethod.HEAD && fs.Length > 0L then
              do! transferStream conn fs
            return conn
        finally
          fs.Dispose()
      }, status

    let task, status = writeStream ()

    { ctx with
        response =
          { ctx.response with
              status = status
              content = SocketTask task } }
    |> succeed

let download : WebPart =
  fun ctx ->
    async {
      let filename = Uri.EscapeDataString("world.txt")

      let createStream () =
        File.Open("./hello.txt", FileMode.Open, FileAccess.Read, FileShare.Read)
        :> Stream

      let webPart : WebPart  =
        sendStream createStream false
        >=> setHeader "Content-Type" "application/octet-stream"
        >=> setHeader "Content-Disposition" $"attachment; filename=\"{filename}\""

      return! webPart ctx
    }

let app =
  choose
    [
      pathCi "/api/download"
      >=> download

      NOT_FOUND "Not found"
    ]

let config =
  {
    defaultConfig with
      bindings =
        [
          HttpBinding.create HTTP IPAddress.Any 5000us
        ]
  }

startWebServer config app
