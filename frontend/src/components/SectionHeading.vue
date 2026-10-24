<script setup lang="ts">
import { ArrowUpRight } from 'lucide-vue-next'
defineProps<{ title: string; eyebrow?: string; description?: string; to?: string; number?: string }>()
</script>
<template><div class="section-heading editorial-heading"><div><span v-if="eyebrow" class="eyebrow">{{ eyebrow }}</span><h2><span v-if="number" class="section-number" aria-hidden="true">{{ number }}</span>{{ title }}</h2><p v-if="description">{{ description }}</p></div><RouterLink v-if="to" :to="to" class="text-link">Xem tất cả<ArrowUpRight :size="18" /></RouterLink><slot /></div></template>