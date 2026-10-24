import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { useAuth } from './stores/auth'
import './assets/main.css'
async function start() { const app = createApp(App); app.use(createPinia()); const auth = useAuth(); await auth.restore(); app.use(router); window.addEventListener('booknest:unauthorized', () => { if (!auth.token) return; const redirect = router.currentRoute.value.fullPath; auth.logout(); if (router.currentRoute.value.path !== '/login') void router.replace({ path: '/login', query: { redirect, expired: '1' } }) }); await router.isReady(); app.mount('#app') }
void start()