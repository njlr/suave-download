const path = require("path");

module.exports = {
  mode: "development",
  devServer: {
    static: {
      directory: path.resolve(__dirname, "./public"),
      publicPath: "/"
    },
    proxy: {
      '/api': 'http://localhost:5000',
    },
  }
}
