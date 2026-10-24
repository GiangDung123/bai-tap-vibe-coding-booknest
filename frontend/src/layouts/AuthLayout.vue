<script setup lang="ts">
import { ArrowLeft, BookOpen } from 'lucide-vue-next'
</script>
<template>
  <div class="auth-layout">
    <a class="skip-link" href="#auth-content">Chuyển đến nội dung</a>
    <header class="auth-header">
      <RouterLink to="/" class="logo" aria-label="BookNest — Trang chủ"><span class="logo-mark"><BookOpen :size="25" :stroke-width="1.6" /></span>Book<span>Nest</span><i></i></RouterLink>
      <RouterLink to="/" class="auth-home"><ArrowLeft :size="17" /> Về trang chủ</RouterLink>
    </header>
    <main id="auth-content" class="auth-content" tabindex="-1"><slot /></main>
    <footer class="auth-footer">BookNest · Một góc nhỏ cho những trang sách.</footer>
  </div>
</template>
<style scoped>
.auth-layout { min-height:100dvh; display:flex; flex-direction:column; background:radial-gradient(ellipse at 12% 40%,#eaf0fc 0,transparent 55%),#F7F8FC; }
.auth-header { width:min(1200px,calc(100% - 64px)); margin-inline:auto; display:flex; align-items:center; justify-content:space-between; gap:16px; padding-block:24px; }
.auth-home { display:inline-flex; align-items:center; gap:8px; min-height:44px; color:#475467; font-size:14px; }
.auth-home:hover { color:#2457D6; }
.auth-content { flex:1; display:flex; align-items:center; justify-content:center; padding:24px 32px 40px; }
.auth-footer { text-align:center; color:#667085; font-size:12px; padding:0 20px 24px; }
@media (max-width:767px) { .auth-header { width:calc(100% - 40px); padding-block:16px; }.auth-header .logo { font-size:24px; }.auth-home { font-size:13px; gap:5px; }.auth-content { padding:16px 20px 32px; } }
@media (max-width:359px) { .auth-header { width:calc(100% - 32px); }.auth-content { padding-inline:16px; }.auth-header .logo { font-size:21px; }.auth-home { font-size:12px; } }
</style>