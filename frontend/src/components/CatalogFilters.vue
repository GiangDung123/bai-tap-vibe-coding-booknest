<script setup lang="ts">
import { Search, X } from 'lucide-vue-next'
import type { Category } from '../types'
import BaseSelect from './BaseSelect.vue'
defineProps<{ filters: { search: string; categoryId: string; minPrice: string; maxPrice: string; sort: string }; categories: Category[]; prefix: string }>()
defineEmits<{ apply: []; reset: [] }>()
</script>
<template>
  <form class="catalog-filter-bar" aria-label="Bộ lọc sách" @submit.prevent="$emit('apply')">
    <div class="field filter-search"><label :for="`${prefix}-search`">Tìm kiếm</label><div class="search-control"><Search :size="16" /><input :id="`${prefix}-search`" v-model="filters.search" placeholder="Tên sách, tác giả…"></div></div>
    <div class="field filter-category"><label :for="`${prefix}-category`">Danh mục</label><BaseSelect :id="`${prefix}-category`" v-model="filters.categoryId" @change="$emit('apply')"><option value="">Tất cả danh mục</option><option v-for="c in categories" :key="c.id" :value="String(c.id)">{{ c.name }}</option></BaseSelect></div>
    <div class="field"><label :for="`${prefix}-min`">Giá từ (đ)</label><input :id="`${prefix}-min`" v-model="filters.minPrice" type="number" min="0" placeholder="0"></div>
    <div class="field"><label :for="`${prefix}-max`">Giá đến (đ)</label><input :id="`${prefix}-max`" v-model="filters.maxPrice" type="number" min="0" placeholder="Không giới hạn"></div>
    <button type="submit" class="btn">Áp dụng</button>
    <button type="button" class="btn secondary" @click="$emit('reset')"><X :size="16" /> Xóa lọc</button>
  </form>
</template>
<style scoped>
.catalog-filter-bar { display:grid; grid-template-columns:minmax(240px,2.2fr) minmax(180px,1.4fr) repeat(2,minmax(130px,1fr)) auto auto; gap:14px; align-items:end; padding:16px; border:1px solid var(--line); border-radius:12px; background:white; }
.field { min-width:0; margin:0; gap:0; }
.field label { display:block; font-size:13px; margin-bottom:6px; }
.catalog-filter-bar input,.catalog-filter-bar :deep(select),.catalog-filter-bar .btn { width:100%; height:42px; min-height:42px; padding:8px 12px; font-size:14px; }
.catalog-filter-bar .btn { white-space:nowrap; gap:6px; }
.search-control { height:42px; }
.search-control input { min-width:0; padding-left:0; }
@media (max-width:1199px) {
  .catalog-filter-bar { grid-template-columns:repeat(4,minmax(0,1fr)); }
  .filter-search,.filter-category { grid-column:span 2; }
}
@media (max-width:767px) {
  .catalog-filter-bar { grid-template-columns:repeat(2,minmax(0,1fr)); gap:12px; }
  .filter-search,.filter-category { grid-column:1/-1; }
}
@media (max-width:359px) { .catalog-filter-bar { padding:12px; gap:12px 8px; } }
</style>