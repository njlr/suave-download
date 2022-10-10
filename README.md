# suave-download

Demonstrates how to create a file download link using Suave.

Fetch the dependencies:

```bash
yarn install
```

Start the server:

```bash
dotnet fsi ./Server.fsx
```

Start the client:

```bash
yarn webpack serve
```

Test the download using `curl`:

```bash
curl localhost:5000/api/download
```
