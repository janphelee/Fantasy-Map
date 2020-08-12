const fs = require('fs')
const path = require('path')

const public = '../godot/public'

function readdir(src, callback) {
  fs.readdir(src, (err, files) => {
    if (err) {
      callback(false)
      return
    }

    if (!files.length) {
      callback(false)
      return
    }

    let dirs = []
    files.forEach(file => {
      fs.stat(path.join(src, file), (err, stats) => {
        dirs.push({ file, err, stats })
        if (dirs.length === files.length) callback(dirs)
      })
    })
  })
}

const pro1 = new Promise((resolve, reject) => {
  let src = 'src'
  readdir(path.resolve(__dirname, src), dirs => {
    if (!dirs) {
      resolve([])
      return
    }
    resolve(
      dirs.filter(
        d => !d.file.startsWith('_') &&
          d.stats.isDirectory()
      ).map(d => {
        return {
          entry: path.resolve(__dirname, `${src}/${d.file}`),
          output: {
            path: path.resolve(__dirname, `${public}/${d.file}`),
            filename: 'index.bundle.js'
          }
        }
      })
    )
  })
})

const VueLoaderPlugin = require('vue-loader/lib/plugin')
const MiniCssExtractPlugin = require('mini-css-extract-plugin')

const pro2 = new Promise((resolve, reject) => {
  let src = 'vue'
  readdir(path.resolve(__dirname, src), dirs => {
    if (!dirs) {
      resolve([])
      return
    }
    let aa = dirs.filter(
      d => !d.file.startsWith('_') &&
        d.stats.isDirectory()
    ).map(d => {
      let entry = path.resolve(__dirname, `${src}/${d.file}`)
      let outry = path.resolve(__dirname, `${public}/${d.file}`)
      return {
        entry: entry,
        output: {
          path: outry,
          filename: 'index.bundle.js'
        },
        resolve: {
          extensions: ['.js', '.json', '.vue'],
          alias: {
            '@': entry
          }
        },
        plugins: [
          new VueLoaderPlugin(),//必需
          new MiniCssExtractPlugin({
            filename: 'style.css'
          })
        ],
        module: {
          rules: [
            {
              test: /\.vue$/,
              loader: 'vue-loader'
            },
            {
              test: /\.js$/,
              exclude: /(node_modules|bower_components)/,
              loader: 'babel-loader'
            },
            {
              test: /\.css$/,
              use: [
                // !production ? 'vue-style-loader' :
                  MiniCssExtractPlugin.loader, 'css-loader'
              ]
            },
            {
              test: /\.(png|jpe?g|gif|svg)(\?.*)?$/,
              loader: 'url-loader',
              options: {
                limit: 10000,
                name: ('img/[name].[hash:7].[ext]')
              }
            },
            {
              test: /\.(mp4|webm|ogg|mp3|wav|flac|aac)(\?.*)?$/,
              loader: 'url-loader',
              options: {
                limit: 10000,
                name: ('media/[name].[hash:7].[ext]')
              }
            },
            {
              test: /\.(woff2?|eot|ttf|otf)(\?.*)?$/,
              loader: 'url-loader',
              options: {
                limit: 10000,
                name: ('fonts/[name].[hash:7].[ext]')
              }
            }
          ]
        }
      }
    })

    resolve(aa)
  })

})

let mode = process.argv[2]
let production = mode.includes('mode=production')

module.exports = new Promise((resolve, reject) => {
  // 不同框架放在下面不同处理
  Promise.all([pro1, pro2]).then(array => {
    var bb = []
    array.forEach(a => bb.push(...a))
    resolve(bb)
  })
})
