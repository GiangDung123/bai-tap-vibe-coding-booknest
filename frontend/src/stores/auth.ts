import { defineStore } from 'pinia'
import { ref } from 'vue'
import { http } from '../api/http'
import type { User } from '../types'
export const useAuth = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref(sessionStorage.getItem('booknest.token') || '')
  function setSession(data: { token: string; user: User }) { token.value = data.token; user.value = data.user; sessionStorage.setItem('booknest.token', data.token) }
  function logout() { token.value = ''; user.value = null; sessionStorage.removeItem('booknest.token') }
  function updateUser(data: User) { user.value = data }
  async function restore() { if (token.value) { try { user.value = (await http.get<User>('/auth/me')).data } catch { logout() } } }
  return { user, token, setSession, logout, restore, updateUser }
})