<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
const props = withDefaults(defineProps<{ open: boolean; title: string; side?: 'left' | 'right' }>(), { side: 'left' })
const emit = defineEmits<{ close: [] }>()
const dialog = ref<HTMLDialogElement>()
let opener: HTMLElement | null = null
let previousOverflow = ''
watch(() => props.open, async open => {
  await nextTick()
  if (open && dialog.value && !dialog.value.open) {
    opener = document.activeElement as HTMLElement
    previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    dialog.value.showModal()
  } else if (!open && dialog.value?.open) {
    dialog.value.close()
    document.body.style.overflow = previousOverflow
    opener?.focus({ preventScroll: true })
  }
}, { immediate: true })
function backdrop(e: MouseEvent) { if (e.target === dialog.value) { const rect = dialog.value.getBoundingClientRect(); if (e.clientX < rect.left || e.clientX > rect.right || e.clientY < rect.top || e.clientY > rect.bottom) emit('close') } }
onBeforeUnmount(() => { if (dialog.value?.open) { dialog.value.close(); document.body.style.overflow = previousOverflow } })
</script>
<template><Teleport to="body"><dialog ref="dialog" class="drawer" :class="`drawer-${side}`" :aria-label="title" @cancel.prevent="emit('close')" @click="backdrop"><header class="drawer-header"><h2>{{ title }}</h2><button type="button" class="icon-btn" autofocus :aria-label="`Đóng ${title.toLowerCase()}`" @click="emit('close')"><X :size="22" /></button></header><div class="drawer-body"><slot /></div></dialog></Teleport></template>