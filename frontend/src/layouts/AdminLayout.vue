<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { BookOpen, LayoutDashboard, Tags, LibraryBig, ShoppingBag, ArrowLeft, Menu, Users } from 'lucide-vue-next'
import Drawer from '../components/Drawer.vue'
import AccountMenu from '../components/AccountMenu.vue'
const route = useRoute(), open = ref(false)
const links = [{ to: '/admin', name: 'Tổng quan', icon: LayoutDashboard }, { to: '/admin/categories', name: 'Danh mục', icon: Tags }, { to: '/admin/products', name: 'Sản phẩm', icon: LibraryBig }, { to: '/admin/orders', name: 'Đơn hàng', icon: ShoppingBag }, { to: '/admin/users', name: 'Người dùng', icon: Users }]
const title = computed(() => route.path === '/admin/profile' ? 'Hồ sơ của tôi' : [...links].reverse().find(l => route.path.startsWith(l.to))?.name || 'Quản trị')
function active(to: string) { return to === '/admin' ? route.path === to : route.path.startsWith(to) }
</script>
<template><div class="admin-shell"><aside class="admin-sidebar"><RouterLink to="/" class="logo"><span class="logo-mark"><BookOpen :size="24" /></span>Book<span>Nest</span></RouterLink><small>KHÔNG GIAN QUẢN TRỊ</small><nav aria-label="Quản trị"><RouterLink v-for="l in links" :key="l.to" :to="l.to" :class="{ 'nav-active': active(l.to) }"><component :is="l.icon" :size="20" />{{ l.name }}</RouterLink></nav><RouterLink to="/" class="back-shop"><ArrowLeft :size="18" /> Về cửa hàng</RouterLink></aside><div class="admin-main"><header class="admin-header"><button class="icon-btn mobile-toggle" aria-label="Menu quản trị" :aria-expanded="open" @click="open = true"><Menu /></button><span>{{ title }} <span class="muted">/ BookNest Admin</span></span><div class="account"><AccountMenu /></div></header><main class="admin-content"><RouterView :key="$route.path" /></main></div></div><Drawer :open="open" title="BookNest Admin" @close="open = false"><nav class="mobile-nav"><RouterLink v-for="l in links" :key="l.to" :to="l.to" @click="open = false"><component :is="l.icon" :size="19" /> {{ l.name }}</RouterLink><RouterLink to="/" @click="open = false">← Về cửa hàng</RouterLink></nav></Drawer></template>