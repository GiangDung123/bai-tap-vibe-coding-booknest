<script setup lang="ts">
const model = defineModel<string | number>()
</script>
<template><select v-model="model" class="ui-select"><slot /></select></template>