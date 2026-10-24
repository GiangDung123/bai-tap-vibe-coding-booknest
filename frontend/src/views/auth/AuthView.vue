<script setup lang="ts">
import { computed, nextTick, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { BookOpen, ArrowRight, Eye, EyeOff, Mail, LockKeyhole, LoaderCircle } from 'lucide-vue-next'
import { useAuth } from '../../stores/auth'
import { http, errorMessage } from '../../api/http'
import { safeRedirect } from '../../utils/format'
import FormField from '../../components/FormField.vue'
const route = useRoute(), router = useRouter(), auth = useAuth(), register = computed(() => route.path === '/register'), showPassword = ref(false), busy = ref(false), error = ref(''), form = reactive({ fullName: '', email: '', password: '', confirmPassword: '' });
const passwordInput = ref<HTMLInputElement>()
async function togglePassword() {
  const input = passwordInput.value
  const start = input?.selectionStart, end = input?.selectionEnd, direction = input?.selectionDirection
  const focused = document.activeElement === input
  showPassword.value = !showPassword.value
  await nextTick()
  if (input && start != null && end != null) input.setSelectionRange(start, end, direction || undefined)
  if (focused) input?.focus({ preventScroll: true })
}
async function submit() { if (busy.value) return; busy.value = true; error.value = ''; try { if (register.value && form.password !== form.confirmPassword) throw new Error('Mật khẩu xác nhận không khớp.'); const { data } = await http.post(register.value ? '/auth/register' : '/auth/login', form); auth.setSession(data); await router.push(safeRedirect(route.query.redirect)); } catch (e) { error.value = errorMessage(e) } finally { busy.value = false; form.password = ''; form.confirmPassword = '' } }
</script>
<template>
  <div class="auth-card">
    <aside class="auth-illustration" aria-label="Không gian của người yêu sách">
      <span class="auth-kicker">KHÔNG GIAN CỦA NGƯỜI YÊU SÁCH</span>
      <h2>Một lần mở sách,<br>muôn vàn khám phá.</h2>
      <p>Đăng nhập để chọn cuốn sách tiếp theo và theo dõi những đơn hàng của bạn.</p>
      <div class="auth-books" aria-hidden="true">
        <div class="book-glow"></div>
        <div class="decor-book book-left"><small>TỦ SÁCH BOOKNEST</small><strong>Những ngày<br>chậm rãi</strong><BookOpen :size="28" :stroke-width="1" /><span>MỘT KHOẢNG TRỜI RIÊNG</span></div>
        <div class="decor-book book-right"><small>TỦ SÁCH BOOKNEST</small><strong>Đi qua<br>những mùa xanh</strong><BookOpen :size="28" :stroke-width="1" /><span>ĐỌC ĐỂ KHÁM PHÁ</span></div>
        <div class="decor-book book-center"><small>TỦ SÁCH BOOKNEST</small><strong>Mở sách.<br>Mở thế giới.</strong><BookOpen :size="36" :stroke-width="1" /><span>DÀNH CHO SỰ TÒ MÒ</span></div>
        <div class="book-shelf"></div>
      </div>
    </aside>
    <section class="auth-form" aria-labelledby="auth-title">
      <div class="auth-form-inner">
        <span class="auth-mobile-brand"><BookOpen :size="18" /> KHÔNG GIAN CỦA NGƯỜI YÊU SÁCH</span>
        <h1 id="auth-title">{{ register ? 'Tạo tài khoản' : 'Chào mừng trở lại' }}</h1>
        <p class="auth-description">{{ register ? 'Bắt đầu hành trình đọc sách cùng BookNest.' : 'Đăng nhập vào tài khoản BookNest của bạn.' }}</p>
        <p v-if="route.query.expired" class="alert" role="status">Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.</p>
        <p v-if="route.query.passwordChanged" class="alert success" role="status">Đổi mật khẩu thành công. Vui lòng đăng nhập lại.</p>
        <p v-if="error" class="alert error" role="alert">{{ error }}</p>
        <form :aria-busy="busy" @submit.prevent="submit">
          <FormField v-if="register" label="Họ và tên" id="fullName"><input id="fullName" v-model="form.fullName" autocomplete="name" required minlength="2" maxlength="120" placeholder="Nhập họ và tên"></FormField>
          <FormField label="Email" id="email"><div class="auth-input"><Mail :size="19" aria-hidden="true" /><input id="email" v-model="form.email" type="email" autocomplete="username" required maxlength="254" placeholder="Nhập địa chỉ email"></div></FormField>
          <FormField label="Mật khẩu" id="password"><div class="auth-input"><LockKeyhole :size="19" aria-hidden="true" /><input id="password" ref="passwordInput" v-model="form.password" :type="showPassword ? 'text' : 'password'" :autocomplete="register ? 'new-password' : 'current-password'" required minlength="8" maxlength="128" :placeholder="register ? 'Ít nhất 8 ký tự' : 'Nhập mật khẩu'" class="auth-password"><button type="button" class="icon-btn password-toggle" :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" :aria-pressed="showPassword" @mousedown.prevent @click="togglePassword"><EyeOff v-if="showPassword" :size="20" /><Eye v-else :size="20" /></button></div></FormField>
          <FormField v-if="register" label="Xác nhận mật khẩu" id="confirm"><input id="confirm" v-model="form.confirmPassword" :type="showPassword ? 'text' : 'password'" autocomplete="new-password" required minlength="8" maxlength="128" placeholder="Nhập lại mật khẩu"></FormField>
          <button type="submit" class="btn auth-submit" :disabled="busy"><LoaderCircle v-if="busy" class="auth-spinner" :size="20" aria-hidden="true" />{{ busy ? (register ? 'Đang tạo tài khoản…' : 'Đang đăng nhập…') : register ? 'Đăng ký tài khoản' : 'Đăng nhập' }}<ArrowRight v-if="!busy" :size="18" aria-hidden="true" /></button>
        </form>
        <p class="auth-switch">{{ register ? 'Đã có tài khoản?' : 'Chưa có tài khoản?' }} <RouterLink :to="{ path: register ? '/login' : '/register', query: route.query }">{{ register ? 'Đăng nhập' : 'Đăng ký ngay' }}</RouterLink></p>
      </div>
    </section>
  </div>
</template>
<style scoped>
.auth-card { display:grid; grid-template-columns:48% 52%; width:100%; max-width:1040px; min-height:610px; border:1px solid #e1e6ef; border-radius:24px; background:white; box-shadow:0 20px 65px #142b5710,0 3px 12px #142b5705; }
.auth-illustration { position:relative; display:flex; flex-direction:column; padding:44px 38px 26px; color:#fffaf0; border-radius:23px 0 0 23px; background:linear-gradient(145deg,#142B57 10%,#244D87 100%); overflow:hidden; }
.auth-kicker { font-size:10px; line-height:1.6; font-weight:600; letter-spacing:1.6px; color:#d7e4fa; }
.auth-illustration h2 { font-family:var(--font-editorial); color:#fffaf0; font-size:40px; font-weight:400; letter-spacing:-.7px; line-height:1.18; margin:23px 0 18px; }
.auth-illustration>p { color:#d7e4fa; max-width:340px; font-size:14px; line-height:1.8; }
.auth-books { position:relative; width:100%; height:260px; margin-top:auto; padding-top:24px; flex-shrink:0; }
.book-glow { position:absolute; width:255px; height:255px; border-radius:50%; background:#ffffff0b; bottom:-5px; left:50%; transform:translateX(-50%); }
.decor-book { position:absolute; width:134px; height:183px; padding:19px 10px 14px; display:flex; flex-direction:column; align-items:center; text-align:center; border-radius:2px 6px 6px 2px; border-left:5px solid #00000015; box-shadow:6px 12px 20px #06183140; color:#283d51; }
.decor-book small { font:6px var(--font-sans); letter-spacing:1px; }
.decor-book strong { font-family:var(--font-editorial); font-size:20px; line-height:1.2; font-weight:400; margin-top:22px; }
.decor-book svg { margin-top:auto; opacity:.7; }
.decor-book>span { margin-top:12px; font-size:5px; letter-spacing:1px; }
.book-left { left:calc(50% - 166px); bottom:20px; transform:rotate(-9deg); background:#c4d8d6; }
.book-right { right:calc(50% - 166px); bottom:20px; transform:rotate(9deg); background:#e4b5a0; }
.book-center { left:50%; bottom:38px; transform:translateX(-50%); height:205px; width:144px; background:#e6c66f; z-index:1; }
.book-center strong { font-size:23px; }
.book-shelf { position:absolute; height:7px; background:#ffffff26; border-radius:2px; bottom:5px; left:2%; right:2%; box-shadow:0 10px 22px #081b3633; }
.auth-form { display:flex; align-items:center; max-width:none; width:100%; padding:48px 52px; }
.auth-form-inner { width:100%; max-width:400px; margin-inline:auto; }
.auth-form h1 { font-size:31px; font-weight:600; letter-spacing:-1px; line-height:1.3; margin:0 0 12px; }
.auth-description { font-size:14px; color:#667085; line-height:1.7; }
.auth-form form { margin-top:28px; }
.auth-form :deep(.field) { gap:8px; margin-bottom:20px; }
.auth-form :deep(.field>label) { font-size:14px; font-weight:600; }
.auth-input { position:relative; }
.auth-input>svg { position:absolute; top:16px; left:14px; color:#667085; pointer-events:none; }
.auth-form input { height:50px; min-height:50px; border:1px solid #D8DEE9; border-radius:11px; color:#182230; background:#fff; font-size:16px; padding:12px 14px; }
.auth-input input { padding-left:44px; }
.auth-input .auth-password { padding-right:50px; }
.auth-form input::placeholder { color:#667085; opacity:1; }
.auth-form input:focus { border-color:#2457D6; outline:0; box-shadow:0 0 0 3px #2457D61a; }
.auth-form input[aria-invalid=true] { border-color:#b42318; }
.auth-form input:autofill { background:#f4f7ff; color:#182230; }
.auth-form input:-webkit-autofill { -webkit-text-fill-color:#182230; -webkit-box-shadow:0 0 0 1000px #f4f7ff inset; caret-color:#182230; }
.auth-form input:-webkit-autofill:focus { -webkit-box-shadow:0 0 0 1000px #f4f7ff inset,0 0 0 3px #2457D61a; }
.password-toggle { position:absolute; top:3px; right:3px; }
.auth-submit { width:100%; height:50px; min-height:50px; margin-top:4px; background:#2457D6; border-color:#2457D6; }
.auth-submit:hover:not(:disabled) { background:#1D46AD; }
.auth-submit:disabled { opacity:1; background:#446dcf; cursor:wait; }
.auth-spinner { animation:auth-spin 1s linear infinite; }
.auth-switch { border-top:1px solid #e9edf4; padding-top:23px; margin-top:26px; text-align:center; color:#667085; font-size:14px; }
.auth-switch a { color:#2457D6; font-weight:600; display:inline-flex; align-items:center; min-height:44px; text-decoration:underline; text-underline-offset:3px; }
.auth-form .alert { font-size:14px; margin:18px 0 0; }
.auth-mobile-brand { display:none; }
@keyframes auth-spin { to { transform:rotate(360deg); } }
@media (max-width:1023px) { .auth-form { padding:36px 30px; }.auth-illustration { padding:36px 28px 24px; }.auth-illustration h2 { font-size:36px; }.auth-kicker { letter-spacing:1px; }.decor-book { width:110px; }.book-left { left:calc(50% - 133px); }.book-right { right:calc(50% - 133px); }.book-center { width:126px; }.auth-form h1 { font-size:28px; } }
@media (max-width:859px) { .auth-card { grid-template-columns:minmax(0,1fr); min-height:0; max-width:520px; }.auth-illustration { display:none; }.auth-form { padding:36px; }.auth-mobile-brand { display:flex; align-items:center; gap:8px; color:#2457D6; font-size:9px; font-weight:600; letter-spacing:.8px; margin-bottom:24px; }.auth-form h1 { font-size:30px; } }
@media (max-width:479px) { .auth-form { padding:28px 22px; }.auth-form h1 { font-size:28px; letter-spacing:-.8px; }.auth-card { border-radius:20px; }.auth-description { font-size:14px; }.auth-mobile-brand { font-size:8px; letter-spacing:.5px; }.auth-switch { font-size:13px; } }
@media (prefers-reduced-motion:reduce) { .auth-spinner { animation:none; } }
</style>