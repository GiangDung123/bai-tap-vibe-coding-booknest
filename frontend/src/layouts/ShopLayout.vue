<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { BookOpen, ShoppingBag, Search, Menu, ArrowRight, LogOut, UserRound } from 'lucide-vue-next'
import { useAuth } from '../stores/auth'
import { useCart } from '../stores/cart'
import ChatWidget from '../components/ChatWidget.vue'
import Drawer from '../components/Drawer.vue'
import AccountMenu from '../components/AccountMenu.vue'
const auth = useAuth(), cart = useCart(), router = useRouter(), mobile = ref(false), search = ref('')
function find() { void router.push({ path: '/products', query: { search: search.value } }); mobile.value = false }
function logout() { mobile.value = false; auth.logout(); void router.push('/') }
</script>
<template>
  <a class="skip-link" href="#main-content">Chuyển đến nội dung</a>
  <div class="announcement">Một cuốn sách hay. Một khoảng trời riêng.</div>
  <header class="site-header">
    <div class="container header-row">
      <RouterLink to="/" class="logo" aria-label="BookNest — Trang chủ"><span class="logo-mark"><BookOpen :size="25" :stroke-width="1.6" /></span>Book<span>Nest</span><i></i></RouterLink>
      <form class="header-search" role="search" @submit.prevent="find"><Search :size="20" /><input v-model="search" aria-label="Tìm sách hoặc tác giả" placeholder="Tìm cuốn sách tiếp theo của bạn…"><button type="submit" class="search-submit" aria-label="Tìm kiếm"><ArrowRight :size="20" /></button></form>
      <div class="header-actions">
        <div v-if="auth.user" class="account"><AccountMenu /><RouterLink v-if="auth.user.role === 'Admin'" to="/admin" class="text-link desktop-account">Quản trị</RouterLink></div>
        <RouterLink v-else to="/login" class="account-link desktop-account"><UserRound :size="22" /><span>Đăng nhập</span></RouterLink>
        <RouterLink to="/cart" class="cart-link" :aria-label="`Giỏ hàng, ${cart.count} cuốn sách`"><ShoppingBag :size="23" /><b v-if="cart.count">{{ cart.count }}</b></RouterLink>
        <button class="icon-btn mobile-toggle" :aria-expanded="mobile" aria-label="Mở menu" @click="mobile = true"><Menu :size="24" /></button>
      </div>
    </div>
    <nav class="container desktop-nav" aria-label="Điều hướng chính"><RouterLink to="/">Trang chủ</RouterLink><RouterLink to="/products" :class="{ active: $route.path.startsWith('/products') }">Khám phá sách</RouterLink><RouterLink to="/products?featured=true">Sách nổi bật</RouterLink><span class="nav-note">Dành một chút thời gian cho những trang sách.</span></nav>
  </header>
  <Drawer :open="mobile" title="Khám phá BookNest" @close="mobile = false"><nav class="mobile-nav"><RouterLink to="/" @click="mobile = false">Trang chủ</RouterLink><RouterLink to="/products" @click="mobile = false">Khám phá sách</RouterLink><RouterLink to="/products?featured=true" @click="mobile = false">Sách nổi bật</RouterLink><RouterLink to="/cart" @click="mobile = false">Giỏ hàng ({{ cart.count }})</RouterLink><RouterLink v-if="auth.user" :to="auth.user.role === 'Admin' ? '/admin/profile' : '/account/profile'" @click="mobile = false">Hồ sơ của tôi</RouterLink><RouterLink v-if="auth.user" to="/orders" @click="mobile = false">Đơn hàng của tôi</RouterLink><RouterLink v-if="auth.user?.role === 'Admin'" to="/admin" @click="mobile = false">Quản trị</RouterLink><button v-if="auth.user" @click="logout">Đăng xuất</button><template v-else><RouterLink to="/login" @click="mobile = false">Đăng nhập</RouterLink><RouterLink to="/register" @click="mobile = false">Trở thành bạn đọc</RouterLink></template></nav><p class="drawer-note">Một góc nhỏ để tìm thấy cuốn sách dành cho bạn.</p></Drawer>
  <main id="main-content" tabindex="-1"><slot /></main>
  <footer class="site-footer"><div class="container footer-grid"><div class="footer-brand"><RouterLink to="/" class="logo"><span class="logo-mark"><BookOpen :size="24" /></span>Book<span>Nest</span></RouterLink><p>Những trang sách mở ra thế giới.<br>Một góc đọc nhỏ, dành riêng cho bạn.</p><span class="footer-signature">Đọc chậm lại. Khám phá nhiều hơn.</span></div><div><h4>Kệ sách</h4><RouterLink to="/products">Tất cả sách</RouterLink><RouterLink to="/products?featured=true">Sách nổi bật</RouterLink><RouterLink to="/cart">Giỏ sách của bạn</RouterLink></div><div><h4>Bạn đọc BookNest</h4><RouterLink to="/orders">Đơn hàng của tôi</RouterLink><RouterLink to="/login">Đăng nhập</RouterLink><RouterLink to="/register">Tạo tài khoản</RouterLink></div><div class="footer-project"><BookOpen :size="26" :stroke-width="1.3" /><h4>Không gian đọc của bạn</h4><p>Website đồ án thương mại điện tử. Thanh toán và vận chuyển được mô phỏng, không giao dịch tiền thật.</p></div></div><div class="container footer-bottom"><span>© {{ new Date().getFullYear() }} BookNest</span><span>Được xây dựng với sự trân trọng dành cho sách.</span></div></footer>
  <ChatWidget />
</template>