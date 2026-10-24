<script setup lang="ts">
import { ref, watch, nextTick, onBeforeUnmount } from 'vue'
const props = defineProps<{ open: boolean; title: string; message: string }>(); const emit = defineEmits<{ confirm: []; close: [] }>(); const dialog = ref<HTMLDialogElement>()
let opener: HTMLElement | null = null
watch(() => props.open, async v => { await nextTick(); if (v && !dialog.value?.open) { opener = document.activeElement as HTMLElement; dialog.value?.showModal() } else if (!v && dialog.value?.open) { dialog.value.close(); opener?.focus({ preventScroll: true }) } }, { immediate: true })
onBeforeUnmount(() => dialog.value?.close())
</script>
<template><Teleport to="body"><dialog ref="dialog" class="confirm-dialog" :aria-label="title" @cancel.prevent="emit('close')"><span class="eyebrow">VUI LÒNG XÁC NHẬN</span><h2>{{ title }}</h2><p>{{ message }}</p><div class="actions"><button type="button" class="btn secondary" autofocus @click="emit('close')">Quay lại</button><button type="button" class="btn danger" @click="emit('confirm')">Xác nhận</button></div></dialog></Teleport></template>