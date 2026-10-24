<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ArrowUpRight, ArrowDown, BookOpen, Code, Leaf, BriefcaseBusiness, Baby, Feather } from 'lucide-vue-next'
import { http, errorMessage } from '../../api/http'
import type { Product, Category, Page } from '../../types'
import ProductCard from '../../components/ProductCard.vue'
import BookCover from '../../components/BookCover.vue'
import SectionHeading from '../../components/SectionHeading.vue'
import Skeleton from '../../components/Skeleton.vue'
const categories = ref<Category[]>([]), featured = ref<Product[]>([]), newest = ref<Product[]>([]), loading = ref(true), error = ref('')
const icons = [Feather, BriefcaseBusiness, Leaf, Code, Baby]
const heroBooks = computed(() => [...new Map([...featured.value, ...newest.value].map(p => [p.id, p])).values()].slice(0, 3))
async function load() {
  loading.value = true; error.value = ''
  try { const [c, f, n] = await Promise.all([http.get<Category[]>('/categories'), http.get<Page<Product>>('/products', { params: { featured: true, pageSize: 4 } }), http.get<Page<Product>>('/products', { params: { sort: 'newest', pageSize: 4 } })]); categories.value = c.data; featured.value = f.data.items; newest.value = n.data.items } catch (e) { error.value = errorMessage(e) } finally { loading.value = false }
}
onMounted(load)
</script>
<template><div class="container home">
  <section class="hero"><div class="hero-copy"><span class="eyebrow"><span class="tiny-dot"></span>BOOKNEST — KHÔNG GIAN ĐỌC CỦA BẠN</span><h1>Mở một cuốn sách.<br>Khám phá <em>một thế giới.</em></h1><p>Từ câu chuyện khiến bạn mỉm cười đến ý tưởng mở ra góc nhìn mới. Cuốn sách tiếp theo đang chờ bạn.</p><div class="hero-actions"><RouterLink class="btn" to="/products">Khám phá sách <ArrowUpRight :size="19" /></RouterLink><a href="#featured" class="hero-secondary">Xem sách nổi bật <ArrowDown :size="16" /></a></div><div class="hero-footnote"><span class="small-rule"></span>Một góc nhỏ cho những khám phá lớn.</div></div>
    <div class="hero-gallery" aria-label="Sách trong tủ sách BookNest"><span class="hero-circle" aria-hidden="true"></span><span class="shelf-number" aria-hidden="true">THE READING ROOM</span><div v-if="loading" class="hero-placeholder skeleton-block"></div><RouterLink v-for="(book, i) in heroBooks" :key="book.id" :to="`/products/${book.id}`" class="hero-book" :class="`hero-book-${i + 1}`" :aria-label="`Khám phá ${book.name}`"><BookCover :book="book" eager /></RouterLink><div v-if="!loading && !heroBooks.length" class="hero-empty"><BookOpen :size="70" :stroke-width="1" /><span>Mỗi cuốn sách, một thế giới.</span></div><span class="shelf-line" aria-hidden="true"></span><span class="gallery-caption">TỪ KỆ SÁCH BOOKNEST <span>↗</span></span></div>
  </section>
  <section v-if="categories.length" class="category-section"><div class="category-intro"><span class="eyebrow">BẮT ĐẦU TỪ ĐIỀU BẠN THÍCH</span><h2>Bạn muốn đọc gì?</h2></div><div class="category-grid"><RouterLink v-for="(c, i) in categories" :key="c.id" :to="{ path: '/products', query: { categoryId: c.id } }" class="category-tile"><component :is="icons[i % icons.length]" :size="22" :stroke-width="1.5" /><strong>{{ c.name }}</strong><ArrowUpRight :size="16" /></RouterLink></div></section>
  <p v-if="error" class="alert error" role="alert">{{ error }} <button @click="load">Thử lại</button></p>
  <section id="featured" class="home-section"><SectionHeading title="Những cuốn sách nổi bật" eyebrow="MỘT GỢI Ý CHO HÔM NAY" number="01" to="/products?featured=true" /><Skeleton v-if="loading" /><div v-else-if="featured.length" class="product-grid"><ProductCard v-for="p in featured" :key="p.id" :product="p" /></div><p v-else-if="!error" class="muted">Sách nổi bật đang được cập nhật. <RouterLink to="/products" class="text-link">Khám phá tất cả sách →</RouterLink></p></section>
  <section class="reading-interlude"><span class="interlude-icon"><BookOpen :size="48" :stroke-width="1" /></span><div><span class="eyebrow">DÀNH MỘT KHOẢNG RIÊNG CHO BẠN</span><h2>Đôi khi, chỉ cần một cuốn sách<br>và một chút thời gian.</h2><p>Không cần vội. Cứ để sự tò mò dẫn lối.</p></div><RouterLink to="/products" class="text-link">Tìm cuốn sách của bạn <ArrowUpRight :size="20" /></RouterLink></section>
  <section class="home-section"><SectionHeading title="Mới trên kệ sách" eyebrow="THÊM MỘT LÝ DO ĐỂ ĐỌC" number="02" to="/products?sort=newest" /><Skeleton v-if="loading" /><div v-else class="product-grid"><ProductCard v-for="p in newest" :key="p.id" :product="p" /></div></section>
</div></template>