import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
export default defineConfig({ plugins: [vue(), tailwindcss()], server: { port: 5173, strictPort: true, proxy: { '/api': 'http://localhost:5080', '/uploads': 'http://localhost:5080', '/images': 'http://localhost:5080' } } })