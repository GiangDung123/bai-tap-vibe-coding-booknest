<script setup lang="ts">
import ShopLayout from './layouts/ShopLayout.vue'
import AuthLayout from './layouts/AuthLayout.vue'
</script>
<template><RouterView v-if="$route.path.startsWith('/admin')" /><AuthLayout v-else-if="$route.meta.authLayout"><RouterView :key="$route.path" /></AuthLayout><ShopLayout v-else><RouterView :key="$route.path" /></ShopLayout></template>