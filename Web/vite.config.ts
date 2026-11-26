import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Адрес Api для dev-режима; в Docker запросы к /api проксирует nginx.
  const apiTarget = env.VITE_API_PROXY ?? 'http://localhost:8080'

  return {
    plugins: [react(), tailwindcss()],
    server: {
      proxy: { '/api': { target: apiTarget, changeOrigin: true } },
    },
  }
})
