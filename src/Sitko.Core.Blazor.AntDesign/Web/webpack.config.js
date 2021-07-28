const path = require('path');
const TerserPlugin = require('terser-webpack-plugin');

const config = {
  entry: {
    fileupload: path.resolve(__dirname, 'src', 'fileupload.js'),
  },
  output: {
    filename: '[name].js',
    path: path.resolve(__dirname, '..', 'wwwroot'),
  },
  module: {
    rules: [
      {
        test: /\.css$/,
        use: ["style-loader", "css-loader"]
      },
      {
        test: /\.(eot|svg|ttf|woff|woff2)$/,
        loader: 'file-loader',
        options: {
          name: '/fonts/[name].[ext]'
        }
      }
    ]
  }
};
module.exports = (env, argv) => {
  if (argv.mode === 'development') {
    config.devtool = 'source-map';
  }

  if (argv.mode === 'production') {
    config.optimization = {
      minimize: true,
      minimizer: [
        new TerserPlugin({
          terserOptions: {
            compress: {
              drop_console: true, // will remove console.logs from your files
            },
          },
        }),
      ],
    }
  }

  return config;
};
