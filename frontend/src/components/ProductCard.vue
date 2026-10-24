<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'
import { ShoppingBag, Star, Check } from 'lucide-vue-next'
import type { Product } from '../types'
import { money } from '../utils/format'
import { useCart } from '../stores/cart'
import BookCover from './BookCover.vue'
defineProps<{ product: Product; compact?: boolean }>()
const cart = useCart(), message = ref(''), added = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined
function add(p: Product) { clearTimeout(timer); try { cart.add(p); message.value = 'Đã thêm vào giỏ'; added.value = true } catch (e) { message.value = (e as Error).message; added.value = false } timer = setTimeout(() => { message.value = ''; added.value = false }, 2800) }
onBeforeUnmount(() => clearTimeout(timer))
</script>
<template><article class="product-card" :class="{ 'product-card-compact': compact }"><RouterLink :to="`/products/${product.id}`" class="book-image" :aria-label="`Xem sách ${product.name}`"><BookCover :book="product" /><span v-if="!product.stockQuantity || !product.isActive" class="badge stock-label">{{ product.isActive ? 'Hết hàng' : 'Ngừng bán' }}</span><span v-else-if="product.isFeatured" class="badge featured-label">Chọn lọc</span></RouterLink><div class="product-copy"><small class="product-category">{{ product.categoryName }}</small><RouterLink :to="`/products/${product.id}`"><h3>{{ product.name }}</h3></RouterLink><p class="product-author">{{ product.author }}</p><div class="product-rating"><span v-if="product.averageRating && product.reviewCount" class="rating"><Star :size="14" fill="currentColor" /> {{ product.averageRating.toFixed(1) }} <span>({{ product.reviewCount }})</span></span></div><div class="product-bottom"><strong>{{ money(product.price) }}</strong><button type="button" class="icon-btn add-btn" :class="{ 'is-added': added }" :aria-label="`Thêm ${product.name} vào giỏ`" :disabled="!product.stockQuantity || !product.isActive" @click.stop="add(product)"><Check v-if="added" :size="20" /><ShoppingBag v-else :size="20" /></button></div><div class="card-feedback" :class="added ? 'success' : 'error'" role="status" aria-live="polite">{{ message }}</div></div></article></template>
<style scoped>
.product-card-compact { min-width:0; display:flex; flex-direction:column; }
.product-card-compact .book-image { height:230px; padding:16px; flex-shrink:0; }
.product-card-compact .product-copy { position:relative; display:flex; flex-direction:column; flex:1; gap:4px; padding-top:10px; }
.product-card-compact .product-category { font-size:11px; line-height:18px; margin:0; overflow:hidden; white-space:nowrap; text-overflow:ellipsis; }
.product-card-compact .product-copy h3 { font-size:16px; line-height:22px; height:44px; min-height:0; margin:0; display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical; overflow:hidden; }
.product-card-compact .product-author { font-size:13px; line-height:20px; margin:0; overflow:hidden; white-space:nowrap; text-overflow:ellipsis; }
.product-card-compact .product-rating { height:auto; min-height:0; margin:0; }
.product-card-compact .product-rating:empty { display:none; }
.product-card-compact .product-bottom { margin-top:auto; padding-top:4px; gap:8px; }
.product-card-compact .product-bottom>strong { font-size:16px; }
.product-card-compact .add-btn { width:44px; height:44px; }
.product-card-compact .card-feedback { position:absolute; top:100%; left:0; right:0; min-height:0; font-size:11px; line-height:16px; }
@media (max-width:767px) {
  .product-card-compact .book-image { height:190px; padding:14px; }
  .product-card-compact .product-copy h3 { font-size:15px; }
  .product-card-compact .product-bottom>strong { font-size:15px; }
}
</style>