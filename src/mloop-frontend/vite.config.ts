import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

export default defineConfig(({ mode }) => {
  const isProd = mode === 'production'
  
  process.stdout.write(`Current mode: ${mode}\n`)
  process.stdout.write(`Is Production? ${isProd}\n`)
  
  return {
    plugins: [react()],
    css: {
      postcss: './postcss.config.js',
    },
    build: {
      minify: 'esbuild',
      esbuild: {
        pure: isProd ? ['console.log'] : [],
      }
    },
    server: {
      proxy: {
        '/api': {
          target: 'https://localhost:7102',
          changeOrigin: true,
          secure: false,
          rewrite: (path) => path.replace(/^\/api/, '/api')
        }
      }
    }
  }
})