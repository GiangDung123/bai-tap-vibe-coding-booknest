// Run after installing Playwright into the ignored .local directory.
const { chromium } = require('../.local/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const config = JSON.parse(fs.readFileSync(path.join(root, 'backend/BookNest.Api/appsettings.Local.json'), 'utf8').replace(/^\uFEFF/, ''));
async function main() {
  fs.mkdirSync(path.join(root, 'docs/screenshots'), { recursive: true });
  const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const errors = []; page.on('pageerror', e => errors.push(e.message));
  const base = 'http://localhost:5173';
  await page.goto(base); await page.locator('.product-card').first().waitFor();
  await page.screenshot({ path: path.join(root, 'docs/screenshots/home-desktop.png'), fullPage: true });
  await page.locator('.product-card .add-btn:not([disabled])').first().click();
  await page.goto(base + '/cart'); await page.locator('.cart-line').waitFor();
  await page.getByRole('link', { name: /Tiến hành đặt hàng/ }).click();
  await page.locator('#email').fill('customer@booknest.local');
  await page.locator('#password').fill(config.Seed.CustomerPassword);
  await page.locator('.auth-form button[type=submit], .auth-form form .btn').click();
  await page.waitForURL('**/checkout');
  await page.locator('#phone').fill('0901234567');
  await page.locator('#address').fill('Địa chỉ kiểm thử giao diện, TP. Hồ Chí Minh');
  await page.getByRole('button', { name: 'Xác nhận đặt hàng' }).click();
  await page.waitForURL('**/payment');
  await page.getByRole('button', { name: 'Mô phỏng thất bại' }).click();
  await page.locator('.badge.failed').waitFor();
  await page.getByRole('button', { name: 'Mô phỏng thành công' }).click();
  await page.locator('.badge.paid').waitFor();
  await page.screenshot({ path: path.join(root, 'docs/screenshots/order-paid.png'), fullPage: true });
  // Admin session uses separate context.
  const admin = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  admin.on('pageerror', e => errors.push(e.message));
  await admin.goto(base + '/login'); await admin.locator('#email').fill('admin@booknest.local'); await admin.locator('#password').fill(config.Seed.AdminPassword); await admin.locator('.auth-form form .btn').click(); await admin.waitForURL(base + '/'); await admin.goto(base + '/admin'); await admin.locator('.stat-card').first().waitFor();
  await admin.screenshot({ path: path.join(root, 'docs/screenshots/admin-desktop.png'), fullPage: true });
  const mobile = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
  mobile.on('pageerror', e => errors.push(e.message));
  for (const url of ['/', '/products', '/cart', '/login']) {
    await mobile.goto(base + url); await mobile.waitForLoadState('networkidle');
    const overflow = await mobile.evaluate(() => document.documentElement.scrollWidth > innerWidth);
    if (overflow) throw new Error('Horizontal overflow: ' + url);
  }
  await mobile.goto(base); await mobile.locator('.product-card').first().waitFor();
  await mobile.getByRole('button', { name: 'Mở menu' }).click(); await mobile.locator('.mobile-nav').waitFor(); await mobile.locator('.mobile-nav').getByRole('link', { name: 'Khám phá sách' }).click(); await mobile.waitForURL('**/products');
  await mobile.locator('.product-card').first().waitFor();
  await mobile.screenshot({ path: path.join(root, 'docs/screenshots/catalog-mobile.png'), fullPage: true });
  if (errors.length) throw new Error(errors.join('\n'));
  await browser.close(); console.log('PASS: desktop/mobile, mobile menu, no horizontal overflow on 4 routes, cart-login-checkout-failed-retry-paid, admin dashboard; no pageerror.');
}
main().catch(e => { console.error(e.message); process.exit(1); });