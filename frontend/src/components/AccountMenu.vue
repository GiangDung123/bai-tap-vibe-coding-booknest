<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ChevronDown, LogOut, UserRound } from 'lucide-vue-next'
import { useAuth } from '../stores/auth'
const auth = useAuth(), router = useRouter(), root = ref<HTMLDetailsElement>()
const profile = computed(() => auth.user?.role === 'Admin' ? '/admin/profile' : '/account/profile')
function close() { root.value?.removeAttribute('open') }
function escape() { close(); root.value?.querySelector('summary')?.focus() }
function logout() { close(); auth.logout(); void router.push('/login') }
</script>
<template><details v-if="auth.user" ref="root" class="account-menu" @keydown.esc.prevent="escape" @focusout="e => { if (!root?.contains(e.relatedTarget as Node)) close() }"><summary :aria-label="`Tài khoản ${auth.user.fullName}`"><span class="account-avatar">{{ auth.user.fullName.trim().charAt(0).toUpperCase() }}</span><span class="account-name">{{ auth.user.fullName }}</span><ChevronDown :size="15" /></summary><nav aria-label="Tài khoản"><RouterLink :to="profile" @click="close"><UserRound :size="17" /> Hồ sơ của tôi</RouterLink><RouterLink v-if="auth.user.role === 'Customer'" to="/orders" @click="close">Đơn hàng của tôi</RouterLink><RouterLink v-else to="/" @click="close">Về cửa hàng</RouterLink><button type="button" @click="logout"><LogOut :size="17" /> Đăng xuất</button></nav></details></template>
<style scoped>
.account-menu { position:relative; min-width:0; }
summary { display:flex; align-items:center; gap:9px; cursor:pointer; list-style:none; min-height:44px; }
summary::-webkit-details-marker { display:none; }
.account-avatar { display:flex; width:34px; height:34px; align-items:center; justify-content:center; background:#eef3ff; color:#2457d6; border-radius:50%; font-weight:600; flex-shrink:0; }
.account-name { max-width:150px; overflow:hidden; white-space:nowrap; text-overflow:ellipsis; font-size:14px; }
nav { position:absolute; right:0; top:calc(100% + 8px); width:210px; padding:8px; border:1px solid var(--line); border-radius:12px; background:white; box-shadow:0 10px 30px #18223018; z-index:80; }
nav a,nav button { display:flex; align-items:center; gap:9px; min-height:44px; padding:9px 12px; font-size:14px; width:100%; border-radius:7px; text-align:left; }
nav a:hover,nav button:hover { background:#eef3ff; color:#2457d6; }
@media(max-width:479px) { .account-name { display:none; } summary { gap:2px; } }
</style>