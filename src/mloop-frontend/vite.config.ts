import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

export default defineConfig({
  plugins: [react()],
  css: {
    postcss: './postcss.config.js',
  },
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7102',  // Remove /api from here
        changeOrigin: true,
        secure: false,  // Add this to bypass certificate validation in development
        rewrite: (path) => path.replace(/^\/api/, '/api')  // Keep /api in the path
      }
    }
  }
});