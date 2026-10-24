<script setup lang="ts">
import { ref } from 'vue'
import { Eye, EyeOff } from 'lucide-vue-next'
defineProps<{ id: string; autocomplete: string; invalid?: boolean }>()
const model = defineModel<string>({ required: true }), visible = ref(false)
</script>
<template><div class="profile-password"><input :id="id" v-model="model" :type="visible ? 'text' : 'password'" :autocomplete="autocomplete" :aria-invalid="invalid || undefined" :aria-describedby="invalid ? `${id}-mismatch` : undefined" required :minlength="autocomplete === 'current-password' ? undefined : 8" maxlength="128"><button type="button" class="icon-btn" :aria-label="`${visible ? 'Ẩn' : 'Hiện'} ${id === 'current-password' ? 'mật khẩu hiện tại' : id === 'new-password' ? 'mật khẩu mới' : 'xác nhận mật khẩu'}`" :aria-pressed="visible" @mousedown.prevent @click="visible = !visible"><EyeOff v-if="visible" :size="19" /><Eye v-else :size="19" /></button></div></template>
<style scoped>.profile-password { position:relative; }.profile-password input { padding-right:50px; }.profile-password button { position:absolute; right:2px; top:2px; }</style>