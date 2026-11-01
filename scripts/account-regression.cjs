// Invoked only by AccountTests with an isolated SQL Server API on port 5081.
const { chromium } = require('../.local/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = (v, m) => { if (!v) throw new Error(m); };
async function main() {
  assert(process.env.BOOKNEST_BROWSER_CUSTOMER_TOKEN && process.env.BOOKNEST_BROWSER_ADMIN_TOKEN, 'Use isolated AccountTests harness');
  const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: true });
  const out = path.resolve(__dirname, '../docs/screenshots/account-management'); fs.mkdirSync(out, { recursive: true });
  const results = [], errors = [];
  async function pageFor(token) {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.on('pageerror', e => errors.push(e.message));
    await page.route(url => url.pathname.startsWith('/api/'), async route => {
      const url = new URL(route.request().url());
      const response = await route.fetch({ url: 'http://localhost:5081' + url.pathname + url.search });
      await route.fulfill({ response });
    });
    await page.addInitScript(t => { sessionStorage.setItem('booknest.token', t); }, token);
    return page;
  }
  async function noOverflow(page) { const info = await page.evaluate(() => ({ ok: document.documentElement.scrollWidth <= innerWidth, width: innerWidth, bad: [...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > innerWidth + 1 && getComputedStyle(e).position !== 'fixed').slice(0, 5).map(e => e.className) })); assert(info.ok, 'Page horizontal overflow: ' + new URL(page.url()).pathname + ' ' + JSON.stringify(info)); }
  async function shot(page, name) { await page.screenshot({ path: path.join(out, name + '.png'), fullPage: true, mask: [page.locator('#current-password,#new-password,#confirm-password,input[type=password]')] }); }
  try {
    const c = await pageFor(process.env.BOOKNEST_BROWSER_CUSTOMER_TOKEN);
    await c.goto('http://localhost:5173/account/profile'); await c.locator('#profile-name').waitFor();
    await c.locator('#profile-name').fill('  Browser   Updated  '); await c.locator('#profile-phone').fill('+44 20 1234 5678'); await c.locator('#profile-address').fill('Default address for isolated browser test');
    await c.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click(); await c.getByRole('status').filter({ hasText: 'Đã lưu thông tin' }).waitFor();
    assert((await c.locator('.account-name').innerText()) === 'Browser Updated', 'Header updates without relogin');
    await c.locator('.account-menu summary').click(); await c.keyboard.press('Escape'); assert(!(await c.locator('.account-menu').getAttribute('open')), 'Escape closes account menu');
    for (const width of [1440, 768, 390]) { await c.setViewportSize({ width, height: 900 }); await noOverflow(c); await shot(c, 'profile-' + width); }
    await c.route('**/api/account/profile', async route => { if (route.request().method() === 'PUT') return route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Controlled save failure' }) }); const response = await route.fetch({ url: 'http://localhost:5081/api/account/profile' }); await route.fulfill({ response }); });
    await c.locator('#profile-name').fill('Retained unsaved name'); await c.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click(); await c.getByRole('alert').filter({ hasText: 'Controlled save failure' }).waitFor(); assert(await c.locator('#profile-name').inputValue() === 'Retained unsaved name', 'Failed save retains input');
    await c.goto('http://localhost:5173/checkout'); await c.locator('#recipient').waitFor(); assert(await c.locator('#recipient').inputValue() === 'Browser Updated', 'Checkout default name'); assert(await c.locator('#address').inputValue() === 'Default address for isolated browser test', 'Checkout default address'); await c.locator('#address').fill('User is editing this address');
    await c.evaluate(() => window.dispatchEvent(new Event('focus'))); assert(await c.locator('#address').inputValue() === 'User is editing this address', 'Checkout edits not overwritten');
    await c.evaluate(() => sessionStorage.setItem('booknest.checkout', JSON.stringify({ fingerprint: 'fixture', payload: { recipientName: 'Restored recipient', recipientPhone: '0901234567', shippingAddress: 'Restored checkout address', note: '' } })));
    await c.reload(); await c.locator('#address').waitFor(); assert(await c.locator('#address').inputValue() === 'Restored checkout address', 'Restored checkout wins over profile');
    results.push('PASS live own profile save/header + checkout defaults/restoration; controlled save-error retains input; responsive 1440/768/390');
    const a = await pageFor(process.env.BOOKNEST_BROWSER_ADMIN_TOKEN);
    await a.goto('http://localhost:5173/admin/users'); await a.locator('.users-table tbody tr').first().waitFor();
    assert(await a.locator('.admin-sidebar').getByRole('link', { name: 'Người dùng', exact: true }).count(), 'Users sidebar');
    await a.getByRole('textbox', { name: 'Tìm người dùng' }).fill('Browser Updated'); await a.getByRole('button', { name: 'Tìm kiếm', exact: true }).click(); await a.locator('.user-total').filter({ hasText: '1 người dùng' }).waitFor();
    for (const width of [1440, 768, 390]) { await a.setViewportSize({ width, height: 900 }); await noOverflow(a); await shot(a, 'users-' + width); }
    await a.getByRole('button', { name: 'Khóa', exact: true }).click(); await a.locator('dialog[open]').waitFor(); await a.keyboard.press('Escape'); assert(await a.locator('dialog[open]').count() === 0, 'Confirmation Escape cancels');
    await a.getByRole('link', { name: 'Chi tiết', exact: true }).click(); await a.getByRole('heading', { name: 'Browser Updated' }).waitFor(); await noOverflow(a); await shot(a, 'user-detail-mobile');
    await a.goto('http://localhost:5173/admin/profile'); await a.locator('#profile-name').waitFor(); await noOverflow(a); await shot(a, 'admin-profile-mobile');
    results.push('PASS live admin list/search/detail/profile, sidebar, confirmation Escape, responsive 1440/768/390');
    await c.goto('http://localhost:5173/account/profile'); await c.locator('#current-password').fill('Account-Test-123!'); await c.locator('#new-password').fill('Browser-Changed-456!'); await c.locator('#confirm-password').fill('Browser-Changed-456!');
    await c.evaluate(() => localStorage.setItem('booknest.cart', '[{"productId":987654,"quantity":2}]'));
    await c.getByRole('button', { name: 'Đổi mật khẩu', exact: true }).click(); await c.waitForURL('**/login?passwordChanged=1');
    await c.getByRole('status').filter({ hasText: 'Đổi mật khẩu thành công' }).waitFor(); assert(await c.evaluate(() => !sessionStorage.getItem('booknest.token')), 'Password success clears auth'); assert(await c.evaluate(() => localStorage.getItem('booknest.cart').includes('987654')), 'Password change preserves cart');
    const quota = await a.evaluate(async () => {
      const statuses = [];
      for (let i = 0; i < 6; i++) { const r = await fetch('/api/account/change-password', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + sessionStorage.getItem('booknest.token') }, body: JSON.stringify({ currentPassword: 'Incorrect-Test-Only', newPassword: 'Unused-Test-123!', confirmPassword: 'Unused-Test-123!' }) }); statuses.push(r.status); }
      return statuses;
    });
    assert(quota.slice(0, 5).every(s => s === 400) && quota[5] === 429, 'Live password endpoint rate limit');
    results.push('PASS live password change redirects with success message, clears auth and preserves cart');
    results.push('PASS live password rate limit: five rejected attempts, sixth HTTP 429 (test admin only)');
    assert(errors.length === 0, errors.join('\n')); fs.writeFileSync(path.join(out, 'results.txt'), results.join('\n')); console.log(results.join('\n'));
  } finally { await browser.close(); }
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });