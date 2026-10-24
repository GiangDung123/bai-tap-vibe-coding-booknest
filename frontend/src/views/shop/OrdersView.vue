<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ArrowUpRight, BookOpen } from 'lucide-vue-next'
import { http, errorMessage } from '../../api/http'
import type { Order, Page } from '../../types'
import { money, dateTime } from '../../utils/format'
import StatusBadge from '../../components/StatusBadge.vue'
import Pagination from '../../components/Pagination.vue'
import LoadingState from '../../components/LoadingState.vue'
import EmptyState from '../../components/EmptyState.vue'
const result = ref<Page<Order>>(), loading = ref(true), error = ref('')
async function load(page = 1) { loading.value = true; error.value = ''; try { result.value = (await http.get<Page<Order>>('/orders/my', { params: { page } })).data } catch (e) { error.value = errorMessage(e) } finally { loading.value = false } }
onMounted(() => load())
</script>
<template><div class="container page"><nav class="breadcrumb" aria-label="Đường dẫn"><RouterLink to="/">Trang chủ</RouterLink> / <span>Đơn hàng của tôi</span></nav><span class="eyebrow">HÀNH TRÌNH ĐỌC CỦA BẠN</span><h1>Đơn hàng của tôi</h1><p class="muted">Theo dõi những cuốn sách bạn đã chọn.</p><LoadingState v-if="loading" /><p v-else-if="error" class="alert error" role="alert">{{ error }} <button @click="load()">Thử lại</button></p><template v-else><EmptyState v-if="!result?.totalItems" title="Bạn chưa có đơn hàng nào"><RouterLink to="/products" class="btn">Khám phá sách</RouterLink></EmptyState><div v-else class="personal-orders"><article v-for="o in result.items" :key="o.id" class="personal-order"><header><div><RouterLink :to="`/orders/${o.id}`"><strong>{{ o.orderCode }}</strong></RouterLink><small>Đặt ngày {{ dateTime(o.createdAt) }} (VN)</small></div><BookOpen :size="25" :stroke-width="1.3" class="muted" /></header><div class="actions"><span><span class="order-label">Đơn hàng</span><StatusBadge :status="o.orderStatus" /></span><span><span class="order-label">Thanh toán</span><StatusBadge :status="o.paymentStatus" /></span></div><footer><span class="muted">Tổng tiền <strong style="color:var(--ink); margin-left:8px">{{ money(o.totalAmount) }}</strong></span><RouterLink :to="`/orders/${o.id}`" class="text-link" aria-label="Chi tiết →">Xem đơn hàng <ArrowUpRight :size="17" /></RouterLink></footer></article></div><Pagination v-if="result" :page="result.page" :total-pages="result.totalPages" @change="load" /></template></div></template>