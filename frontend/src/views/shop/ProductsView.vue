<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { X } from 'lucide-vue-next'
import { http, errorMessage } from '../../api/http'
import type { Category, Product, Page } from '../../types'
import ProductCard from '../../components/ProductCard.vue'
import Skeleton from '../../components/Skeleton.vue'
import EmptyState from '../../components/EmptyState.vue'
import Pagination from '../../components/Pagination.vue'
import CatalogFilters from '../../components/CatalogFilters.vue'
import BaseSelect from '../../components/BaseSelect.vue'
const route = useRoute(), router = useRouter(), categories = ref<Category[]>([]), result = ref<Page<Product>>(), loading = ref(true), error = ref('')
const filters = reactive({ search: '', categoryId: '', minPrice: '', maxPrice: '', sort: 'newest' })
let timer: ReturnType<typeof setTimeout> | undefined, request = 0, syncing = false
const chips = computed(() => Object.entries(route.query).filter(([key, value]) => ['search', 'categoryId', 'minPrice', 'maxPrice', 'featured'].includes(key) && value).map(([key, value]) => ({ key, label: key === 'categoryId' ? categories.value.find(c => String(c.id) === String(value))?.name || 'Danh mục đã chọn' : key === 'search' ? `Tìm: ${value}` : key === 'minPrice' ? `Từ ${Number(value).toLocaleString('vi-VN')}đ` : key === 'maxPrice' ? `Đến ${Number(value).toLocaleString('vi-VN')}đ` : 'Sách nổi bật' })))
async function load() {
  clearTimeout(timer)
  const n = ++request; loading.value = true; error.value = ''; syncing = true
  for (const key of Object.keys(filters) as (keyof typeof filters)[]) filters[key] = String(route.query[key] || (key === 'sort' ? 'newest' : ''))
  try { const data = (await http.get<Page<Product>>('/products', { params: { ...route.query, pageSize: 20 } })).data; if (n === request) result.value = data } catch (e) { if (n === request) error.value = errorMessage(e) } finally { if (n === request) { loading.value = false; syncing = false } }
}
function apply() {
  clearTimeout(timer)
  if ((filters.minPrice && Number(filters.minPrice) < 0) || (filters.maxPrice && Number(filters.maxPrice) < 0) || (filters.minPrice && filters.maxPrice && Number(filters.minPrice) > Number(filters.maxPrice))) { error.value = 'Khoảng giá không hợp lệ. Giá từ phải nhỏ hơn hoặc bằng giá đến.'; return }
  const query: Record<string, string> = {}
  for (const [key, value] of Object.entries(filters)) if (value) query[key] = value
  if (route.query.featured) query.featured = String(route.query.featured)
  void router.push({ path: '/products', query })
}
function reset() { clearTimeout(timer); Object.assign(filters, { search: '', categoryId: '', minPrice: '', maxPrice: '', sort: 'newest' }); error.value = ''; void router.push('/products') }
function remove(key: string) { const query = { ...route.query }; delete query[key]; delete query.page; void router.push({ query }) }
watch(() => filters.search, () => { if (!syncing) { clearTimeout(timer); timer = setTimeout(apply, 400) } })
watch(() => route.query, load)
onMounted(async () => { await Promise.all([load(), (async () => { try { categories.value = (await http.get<Category[]>('/categories')).data } catch (e) { error.value = errorMessage(e) } })()]) })
onBeforeUnmount(() => { clearTimeout(timer); request++ })
</script>
<template>
  <div class="page-banner"><div class="container catalog-container"><nav class="breadcrumb" aria-label="Đường dẫn"><RouterLink to="/">Trang chủ</RouterLink><span>/</span><span>Tủ sách</span></nav><span class="eyebrow">TỦ SÁCH BOOKNEST</span><h1>Tìm cuốn sách dành cho bạn</h1><p>Mỗi sở thích, một thế giới để khám phá.</p></div></div>
  <div class="container catalog-page">
    <CatalogFilters :filters="filters" :categories="categories" prefix="catalog" @apply="apply" @reset="reset" />
    <section class="catalog-content" aria-label="Kết quả tìm sách" :aria-busy="loading">
      <div class="catalog-toolbar"><p role="status"><template v-if="loading">Đang tìm sách…</template><template v-else><strong>{{ result?.totalItems || 0 }}</strong> cuốn sách được tìm thấy</template></p><label class="sort-label">Sắp xếp <BaseSelect v-model="filters.sort" @change="apply"><option value="newest">Mới nhất</option><option value="priceAsc">Giá tăng dần</option><option value="priceDesc">Giá giảm dần</option></BaseSelect></label></div>
      <div v-if="chips.length" class="filter-chips" aria-label="Bộ lọc đang áp dụng"><button v-for="chip in chips" :key="chip.key" class="filter-chip" :aria-label="`Bỏ lọc ${chip.label}`" @click="remove(chip.key)">{{ chip.label }}<X :size="14" /></button></div>
      <Skeleton v-if="loading" :count="20" catalog /><p v-else-if="error" class="alert error" role="alert">{{ error }} <button @click="load">Thử lại</button></p>
      <template v-else><div v-if="result?.items.length" class="product-grid catalog-grid"><ProductCard v-for="p in result.items" :key="p.id" :product="p" compact /></div><EmptyState v-else title="Chưa tìm thấy cuốn sách phù hợp" text="Thử từ khóa khác hoặc mở rộng khoảng giá."><button class="btn secondary" @click="reset">Xóa bộ lọc</button></EmptyState><Pagination v-if="result" :page="result.page" :total-pages="result.totalPages" @change="(page: number) => router.push({ query: { ...route.query, page } })" /></template>
    </section>
  </div>
</template>
<style scoped>
.catalog-container,.catalog-page { width:min(1440px,calc(100% - 64px)); }
.catalog-page { display:flex; flex-direction:column; gap:20px; }
.catalog-content { width:100%; min-width:0; }
.catalog-toolbar { margin-bottom:18px; }
.catalog-toolbar .sort-label { width:auto; }
.catalog-toolbar :deep(select) { min-height:42px; height:42px; padding-block:8px; }
.catalog-page :deep(.catalog-grid) { grid-template-columns:repeat(2,minmax(0,1fr)); gap:28px 14px; }
.catalog-page :deep(.skeleton-cover) { height:190px; }
@media (min-width:768px) { .catalog-page :deep(.catalog-grid) { grid-template-columns:repeat(3,minmax(0,1fr)); column-gap:20px; } .catalog-page :deep(.skeleton-cover) { height:230px; } }
@media (min-width:1024px) { .catalog-page :deep(.catalog-grid) { grid-template-columns:repeat(4,minmax(0,1fr)); } }
@media (min-width:1440px) { .catalog-page :deep(.catalog-grid) { grid-template-columns:repeat(5,minmax(0,1fr)); } }
@media (max-width:767px) {
  .catalog-container,.catalog-page { width:calc(100% - 32px); }
  .catalog-toolbar { align-items:center; gap:10px; }
  .catalog-toolbar>p { font-size:13px; }
  .catalog-toolbar .sort-label { font-size:12px; gap:6px; }
  .catalog-toolbar :deep(select) { max-width:135px; font-size:13px; padding-inline:8px; }
}
@media (max-width:359px) { .catalog-page :deep(.catalog-grid) { grid-template-columns:minmax(0,1fr); } }
</style>