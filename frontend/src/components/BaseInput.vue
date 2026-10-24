<script setup lang="ts">
const model = defineModel<string | number>()
withDefaults(defineProps<{ type?: string }>(), { type: 'text' })
</script>
<template><input v-model="model" :type="type" class="ui-input"></template>