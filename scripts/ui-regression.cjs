// Read-only API/browser regression. Cart edits stay in this isolated browser.
const { chromium } = require('../.local/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const config = JSON.parse(fs.readFileSync(path.join(root, 'backend/BookNest.Api/appsettings.Local.json'), 'utf8').replace(/^\uFEFF/, ''));
const out = path.join(root, 'docs/screenshots/ui-refresh');
const base = 'http://localhost:5173';
function assert(value, text) { if (!value) throw new Error(text); }
async function main() {
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: true });
  const errors = [], results = [];
  async function shot(page, name) { await page.screenshot({ path: path.join(out, name + '.png'), fullPage: true, mask: [page.locator('#password, #confirm, input[type=password]')] }); }
  async function overflow(page, name) { assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Horizontal overflow: ' + name); }
  try {
    for (const width of [390, 768, 1440]) {
      const page = await browser.newPage({ viewport: { width, height: 950 } });
      page.on('pageerror', e => errors.push(e.message));
      await page.goto(base); await page.locator('.product-card').first().waitFor();
      await overflow(page, 'home ' + width); await shot(page, 'home-' + width);
      assert(await page.locator('.hero-book').count() > 0, 'Hero uses real catalog');
      await page.locator('.product-card .add-btn:not([disabled])').first().click();
      assert(new URL(page.url()).pathname === '/', 'Cart button must not navigate');
      if (width < 901) {
        const trigger = page.getByRole('button', { name: 'Mở menu', exact: true });
        await trigger.click(); await page.getByRole('dialog').waitFor();
        await page.keyboard.press('Escape');
        assert(await trigger.evaluate(el => document.activeElement === el), 'Menu restores focus');
        await trigger.click(); await page.locator('.mobile-nav').getByRole('link', { name: 'Khám phá sách', exact: true }).click();
      } else await page.goto(base + '/products');
      await page.locator('.product-card').first().waitFor(); await overflow(page, 'catalog ' + width);
      await page.locator('#catalog-category').selectOption({ index: 1 });
      await page.waitForURL(/categoryId=/); await page.waitForLoadState('networkidle');
      assert(await page.locator('.filter-chip').count() > 0, 'Active filter chip');
      await page.locator('.filter-chip').first().click(); await page.waitForURL(url => !url.searchParams.has('categoryId'));
      const search = page.getByRole('textbox', { name: 'Tìm sách hoặc tác giả' });
      await search.fill('khong-co-sach-ui-2026'); await search.press('Enter');
      await page.getByRole('heading', { name: 'Chưa tìm thấy cuốn sách phù hợp' }).waitFor();
      await page.locator('.state').getByRole('button', { name: 'Xóa bộ lọc' }).click();
      await page.locator('.product-card').first().waitFor(); await shot(page, 'catalog-' + width);
      await page.locator('.product-card .book-image').first().click(); await page.locator('.detail-copy').waitFor();
      await overflow(page, 'detail ' + width); await shot(page, 'detail-' + width);
      await page.goto(base + '/cart'); await page.locator('.cart-line').waitFor();
      await page.locator('.quantity-label input').first().fill('2'); await page.locator('.quantity-label input').first().press('Tab');
      assert(await page.locator('.quantity-label input').first().inputValue() === '2', 'Quantity updates');
      await page.getByRole('button', { name: 'Xóa giỏ', exact: true }).click();
      await page.getByRole('dialog').waitFor(); await page.keyboard.press('Escape');
      assert(await page.locator('.cart-line').count() > 0, 'Cancel clear preserves cart');
      await overflow(page, 'cart ' + width); await shot(page, 'cart-' + width);
      await page.getByRole('link', { name: /Tiến hành đặt hàng/ }).click(); await page.locator('#email').waitFor();
      assert(new URL(page.url()).searchParams.get('redirect') === '/checkout', 'Checkout redirect preserved');
      await page.locator('#email').fill('customer@booknest.local');
      await page.locator('#password').fill(config.Seed.CustomerPassword);
      await page.getByRole('button', { name: 'Hiện mật khẩu' }).click();
      assert(await page.locator('#password').getAttribute('type') === 'text', 'Show password');
      await page.getByRole('button', { name: 'Ẩn mật khẩu' }).click();
      await shot(page, 'login-' + width); await page.locator('.auth-form button[type=submit]').click();
      await page.waitForURL('**/checkout'); await page.locator('#phone').waitFor(); await overflow(page, 'checkout ' + width);
      await page.getByRole('button', { name: 'Xác nhận đặt hàng' }).click();
      assert(await page.locator('#phone').getAttribute('aria-invalid') === 'true', 'Inline form validation');
      await shot(page, 'checkout-' + width);
      await page.goto(base + '/orders'); await page.waitForLoadState('networkidle'); await overflow(page, 'orders ' + width); await shot(page, 'orders-' + width);
      const orderLink = page.locator('.personal-order footer .text-link').first();
      if (await orderLink.count()) { await orderLink.click(); await page.locator('.order-item').first().waitFor(); await shot(page, 'order-detail-' + width); await overflow(page, 'order detail ' + width); }
      await page.getByRole('button', { name: 'Tư vấn chọn sách', exact: true }).click(); await page.locator('#chat-input').waitFor();
      assert(await page.getByRole('button', { name: 'Gửi câu hỏi' }).isDisabled(), 'Empty chat disabled');
      await shot(page, 'chat-' + width); await page.keyboard.press('Escape');
      await page.close(); results.push('PASS customer responsive + interactions ' + width);
    }
    const admin = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    admin.on('pageerror', e => errors.push(e.message));
    await admin.goto(base + '/login'); await admin.locator('#email').fill('admin@booknest.local'); await admin.locator('#password').fill(config.Seed.AdminPassword); await admin.locator('.auth-form button[type=submit]').click(); await admin.waitForURL(base + '/');
    for (const width of [1440, 768, 390]) {
      await admin.setViewportSize({ width, height: 1000 });
      for (const route of ['/admin', '/admin/products', '/admin/categories', '/admin/orders']) {
        await admin.goto(base + route); await admin.waitForLoadState('networkidle'); await overflow(admin, route + ' ' + width); await shot(admin, route.replaceAll('/', '-').slice(1) + '-' + width);
      }
      await admin.goto(base + '/admin/products'); await admin.waitForLoadState('networkidle'); await admin.getByRole('button', { name: 'Thêm sách', exact: true }).click(); await admin.locator('#p-name').waitFor(); await overflow(admin, 'admin form ' + width); await shot(admin, 'admin-product-form-' + width);
      if (width < 901) { const button = admin.getByRole('button', { name: 'Menu quản trị' }); await button.click(); await admin.keyboard.press('Escape'); assert(await button.evaluate(el => el === document.activeElement), 'Admin menu restores focus'); }
      results.push('PASS admin read-only ' + width);
    }
    assert(errors.length === 0, errors.join('\n'));
    results.push('PASS no pageerror; no order created; no catalog/database writes');
    fs.writeFileSync(path.join(out, 'results.txt'), results.join('\n')); console.log(results.join('\n'));
  } finally { await browser.close(); }
}
main().catch(e => { console.error(e.message); process.exit(1); });