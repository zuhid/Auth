import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'
import istanbul from 'vite-plugin-istanbul'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    ...(process.env.COVERAGE ? [istanbul({
      include: 'src/**',
      exclude: ['node_modules', 'tests/'],
      extension: ['.js', '.vue'],
      requireEnv: false,
    })] : []),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Product',
        short_name: 'Product',
        start_url: '/',
        display: 'standalone',
        background_color: '#ffffff',
        theme_color: '#42b883',
        icons: [
          { src: '/pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: '/pwa-512x512.png', sizes: '512x512', type: 'image/png' }
        ]
      },
      includeAssets: [
        "fonts/Roboto-Regular.ttf"
      ]
    })
  ],
  server: {
    port: 5201
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
})
