<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { BookOpen } from 'lucide-vue-next'
const props = defineProps<{ book: { name: string; author: string; categoryId?: number; images?: string[] }; image?: string; eager?: boolean }>()
const failed = ref(false)
const url = computed(() => props.image || props.book.images?.[0] || '')
const illustrated = computed(() => failed.value || !url.value || /\/images\/cover-[1-5]\.png(?:\?.*)?$/.test(url.value) || url.value.includes('fallback.svg'))
watch(url, () => { failed.value = false })
const tone = computed(() => ((props.book.categoryId || 1) - 1) % 5)
</script>
<template><span class="cover-object" :class="[{ 'type-cover': illustrated }, `cover-tone-${tone}`]" :role="illustrated ? 'img' : undefined" :aria-label="illustrated ? `Bìa minh họa: ${book.name}, ${book.author}` : undefined"><template v-if="illustrated"><span class="cover-edition">BOOKNEST · TỦ SÁCH CỦA BẠN</span><span class="cover-title">{{ book.name }}</span><span class="cover-author">{{ book.author }}</span><span class="cover-illustration" aria-hidden="true"><span></span><BookOpen :size="26" :stroke-width="1" /></span><span class="cover-footnote">BÌA MINH HỌA</span></template><img v-else :src="url" :alt="`Bìa sách ${book.name}`" :loading="eager ? 'eager' : 'lazy'" width="400" height="560" @error="failed = true"></span></template>