/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: parseInt(process.env.PORT || '5173', 10),
    strictPort: true,
    proxy: {
      '/api': {
        target: process.env.VITE_API_BASE_URL || 'https://127.0.0.1:5001',
        changeOrigin: true,
        secure: false,
      }
    }
  },
})
