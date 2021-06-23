const path = require('path');
const webpack = require('webpack');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssoWebpackPlugin = require('csso-webpack-plugin').default;
const TerserPlugin = require('terser-webpack-plugin');

const config = {
    entry: {
        index: path.resolve(__dirname, 'src', 'index.js'),
    },
    output: {
        filename: 'Sitko.Core.Blazor.AntDesign.js',
        path: path.resolve(__dirname, '..', 'wwwroot'),
    },
    plugins: [
        new webpack.ProvidePlugin({}),
        new MiniCssExtractPlugin({
            filename: "Sitko.Blockly.AntDesign.css"
        }),
        new CssoWebpackPlugin()
    ],
    module: {
        rules: [
            {
                test: /\.css$/,
                use: [
                    {
                        loader: MiniCssExtractPlugin.loader,
                        options: {
                            publicPath: '/dist'
                        }
                    },
                    {
                        loader: 'css-loader', // translates CSS into CommonJS modules
                    }]
            },
            {
                test: /\.(eot|svg|ttf|woff|woff2)$/,
                loader: 'file-loader',
                options: {
                    name: '/fonts/[name].[ext]'
                }
            },
            {
                test: /\.(jpg|png|bmp|gif|ico|jpeg)$/,
                loader: 'file-loader',
                options: {
                    name: '/resources/[name].[ext]'
                }
            },
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
