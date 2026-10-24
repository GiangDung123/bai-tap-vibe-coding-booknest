<script setup lang="ts">
withDefaults(defineProps<{ variant?: 'primary' | 'secondary' | 'danger'; loading?: boolean; disabled?: boolean; type?: 'button' | 'submit' | 'reset' }>(), { variant: 'primary', type: 'button' })
</script>
<template><button :type="type" class="btn" :class="variant === 'primary' ? '' : variant" :disabled="disabled || loading" :aria-busy="loading || undefined"><span v-if="loading" role="presentation">◌</span><slot /></button></template>