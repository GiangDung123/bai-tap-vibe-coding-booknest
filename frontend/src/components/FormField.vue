<script setup lang="ts">
import { ref } from 'vue'
defineProps<{ label: string; id: string; hint?: string }>()
const validation = ref('')
function invalid(event: Event) {
  const el = event.target as HTMLInputElement
  validation.value = el.validity.valueMissing ? 'Vui lòng nhập thông tin này.' : el.validity.typeMismatch ? 'Vui lòng nhập đúng định dạng.' : el.validity.tooShort ? `Vui lòng nhập ít nhất ${el.minLength} ký tự.` : el.validity.rangeUnderflow || el.validity.rangeOverflow ? 'Giá trị nằm ngoài khoảng cho phép.' : el.validity.patternMismatch ? 'Thông tin chưa đúng định dạng yêu cầu.' : el.validationMessage
  el.setAttribute('aria-invalid', 'true'); el.setAttribute('aria-describedby', `${el.id}-error`)
}
function update(event: Event) { const el = event.target as HTMLInputElement; if (el.validity.valid) { validation.value = ''; el.removeAttribute('aria-invalid'); el.removeAttribute('aria-describedby') } }
</script>
<template><div class="field" @invalid.capture="invalid" @input="update" @change="update"><label :for="id">{{ label }}</label><slot /><small v-if="validation" :id="`${id}-error`" class="field-error" role="alert">{{ validation }}</small><small v-if="hint" :id="`${id}-hint`">{{ hint }}</small></div></template>