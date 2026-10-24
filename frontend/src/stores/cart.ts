import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { http } from '../api/http'
import type { Product } from '../types'
interface Line { productId: number; quantity: number }
export const useCart = defineStore('cart', () => {
  const lines = ref<Line[]>([])
  try { const raw: unknown = JSON.parse(localStorage.getItem('booknest.cart') || '[]'); if (Array.isArray(raw)) lines.value = raw.filter(i => Number.isInteger(i?.productId) && i.productId > 0 && Number.isInteger(i?.quantity) && i.quantity > 0).slice(0, 100) } catch { /* Corrupt local storage is treated as an empty cart. */ }
  const products = ref<Record<number, Product>>({}), notices = ref<string[]>([])
  let seenPrices: Record<number, number> = {}
  try { seenPrices = JSON.parse(sessionStorage.getItem('booknest.cartPrices') || '{}') } catch { seenPrices = {} }
  watch(products, v => { for (const p of Object.values(v)) seenPrices[p.id] = p.price; sessionStorage.setItem('booknest.cartPrices', JSON.stringify(seenPrices)) }, { deep: true })
  const count = computed(() => lines.value.reduce((s, i) => s + i.quantity, 0))
  const total = computed(() => lines.value.reduce((s, i) => s + (products.value[i.productId]?.price || 0) * i.quantity, 0))
  const valid = computed(() => lines.value.length > 0 && lines.value.every(i => products.value[i.productId]?.isActive && products.value[i.productId]!.stockQuantity >= i.quantity))
  watch(lines, v => localStorage.setItem('booknest.cart', JSON.stringify(v.map(i => ({ productId: i.productId, quantity: i.quantity })))), { deep: true })
  function add(p: Product, qty = 1) { const old = lines.value.find(i => i.productId === p.id); if (!Number.isInteger(qty) || qty < 1 || !p.isActive || (old?.quantity || 0) + qty > p.stockQuantity) throw new Error('Số lượng vượt tồn kho hoặc sách đã ngừng bán.'); products.value[p.id] = p; if (old) old.quantity += qty; else lines.value.push({ productId: p.id, quantity: qty }) }
  function remove(id: number) { lines.value = lines.value.filter(i => i.productId !== id) }
  function setQuantity(id: number, qty: number) { const i = lines.value.find(i => i.productId === id); if (i) i.quantity = Math.max(1, Math.min(1000, Math.floor(qty || 1))) }
  async function refresh() { notices.value = []; await Promise.all(lines.value.map(async i => { try { const p = (await http.get<Product>(`/products/${i.productId}`)).data; if (seenPrices[p.id] !== undefined && seenPrices[p.id] !== p.price) notices.value.push(`Giá ${p.name} đã thay đổi. Giá mới đang được hiển thị.`); products.value[p.id] = p; if (!p.isActive || p.stockQuantity < i.quantity) notices.value.push(`${p.name}: ngừng bán hoặc không đủ tồn kho.`) } catch (e) { delete products.value[i.productId]; throw e } })) }
  return { lines, products, notices, count, total, valid, add, remove, setQuantity, refresh }
})