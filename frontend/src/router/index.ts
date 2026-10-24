import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '../stores/auth'
export const router = createRouter({ history: createWebHistory(), scrollBehavior: () => ({ top: 0 }), routes: [
  { path: '/', component: () => import('../views/shop/HomeView.vue') },
  { path: '/products', component: () => import('../views/shop/ProductsView.vue') },
  { path: '/products/:id', component: () => import('../views/shop/ProductDetailView.vue') },
  { path: '/cart', component: () => import('../views/shop/CartView.vue') },
  { path: '/checkout', component: () => import('../views/shop/CheckoutView.vue'), meta: { auth: true } },
  { path: '/account/profile', component: () => import('../views/account/ProfileView.vue'), meta: { auth: true } },
  { path: '/orders', component: () => import('../views/shop/OrdersView.vue'), meta: { auth: true } },
  { path: '/orders/:id', component: () => import('../views/shop/OrderDetailView.vue'), meta: { auth: true } },
  { path: '/orders/:id/payment', component: () => import('../views/shop/OrderDetailView.vue'), meta: { auth: true } },
  { path: '/login', component: () => import('../views/auth/AuthView.vue'), meta: { authLayout: true } },
  { path: '/register', component: () => import('../views/auth/AuthView.vue'), meta: { authLayout: true } },
  { path: '/admin', component: () => import('../layouts/AdminLayout.vue'), meta: { auth: true, admin: true }, children: [
    { path: '', component: () => import('../views/admin/DashboardView.vue') },
    { path: 'categories', component: () => import('../views/admin/CategoriesView.vue') },
    { path: 'products', component: () => import('../views/admin/ProductsAdminView.vue') },
    { path: 'orders', component: () => import('../views/admin/OrdersAdminView.vue') },
    { path: 'users', component: () => import('../views/admin/UsersView.vue') },
    { path: 'users/:id', component: () => import('../views/admin/UserDetailView.vue') },
    { path: 'profile', component: () => import('../views/account/ProfileView.vue') },
    { path: 'orders/:id', component: () => import('../views/shop/OrderDetailView.vue') }
  ] },
  { path: '/:pathMatch(.*)*', component: () => import('../views/shop/NotFoundView.vue') }
] })
router.beforeEach(to => { const auth = useAuth(); if (to.meta.auth && !auth.user) return { path: '/login', query: { redirect: to.fullPath } }; if (to.meta.admin && auth.user?.role !== 'Admin') return '/' })