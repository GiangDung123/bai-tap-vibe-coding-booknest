// Read-only catalog checks. Falls back explicitly to browser fixtures if local DB is unavailable.
const { chromium } = require('../.local/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const fixture = process.argv.includes('--fixtures');
const out = path.resolve(__dirname, '../docs/screenshots/catalog-horizontal', fixture ? 'fixtures' : '.');
const base = 'http://localhost:5173';
const assert = (value, message) => { if (!value) throw new Error(message); };
async function main() {
  fs.mkdirSync(out, { recursive: true });
  let live = false;
  if (!fixture) try { const r = await fetch('http://localhost:5080/api/products?pageSize=20', { signal: AbortSignal.timeout(6000) }); live = r.ok; } catch {}
  const results = [live ? 'MODE live API (GET only)' : 'MODE browser fixtures (explicit or API unavailable); NOT live API validation'];
  const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: true });
  const errors = [];
  try {
    for (const width of [390, 768, 1280, 1536, 320]) {
      const page = await browser.newPage({ viewport: { width, height: 1000 } });
      const requests = [];
      page.on('pageerror', e => errors.push(e.message));
      page.on('request', r => { if (/\/api\/products\?/.test(r.url())) requests.push(new URL(r.url())); });
      if (!live) await page.route('**/api/**', async route => {
        const u = new URL(route.request().url());
        if (!u.pathname.startsWith('/api/')) return route.continue();
        assert(route.request().method() === 'GET', 'No server mutations allowed');
        if (u.pathname === '/api/categories') return route.fulfill({ json: [{ id: 1, name: 'Văn học' }, { id: 2, name: 'Công nghệ' }] });
        if (u.pathname !== '/api/products') return route.fulfill({ json: {} });
        const q = u.searchParams;
        let items = Array.from({ length: 47 }, (_, i) => ({ id: i + 1, name: i === 0 ? 'Một cuốn sách có tên rất dài để kiểm tra bố cục và giới hạn hai dòng trên màn hình nhỏ' : `Cuốn sách ${i + 1}`, author: 'Tác giả minh họa', categoryId: i % 2 + 1, categoryName: i % 2 ? 'Công nghệ' : 'Văn học', price: 60000 + i * 5000, stockQuantity: i === 0 ? 0 : 20, isActive: true, isFeatured: i % 3 === 1, images: [], reviewCount: i % 4 === 0 ? 2 : 0, averageRating: 4.5 }));
        if (q.get('search')) items = items.filter(p => p.name.includes(q.get('search')) || p.author.includes(q.get('search')));
        if (q.get('categoryId')) items = items.filter(p => p.categoryId === Number(q.get('categoryId')));
        if (q.get('minPrice')) items = items.filter(p => p.price >= Number(q.get('minPrice')));
        if (q.get('maxPrice')) items = items.filter(p => p.price <= Number(q.get('maxPrice')));
        if (q.get('sort') === 'priceAsc') items.sort((a, b) => a.price - b.price);
        if (q.get('sort') === 'priceDesc') items.sort((a, b) => b.price - a.price);
        const totalItems = items.length, pageNumber = Number(q.get('page') || 1), pageSize = Number(q.get('pageSize') || 12);
        return route.fulfill({ json: { items: items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize), totalItems, page: pageNumber, pageSize, totalPages: Math.ceil(totalItems / pageSize) } });
      });
      async function ready() { await page.waitForFunction(() => document.querySelector('.catalog-content')?.getAttribute('aria-busy') === 'false'); }
      async function noOverflow() { assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Overflow at ' + width); }
      await page.goto(base + '/products'); await ready();
      assert(await page.locator('.product-card').count() === 20, '20 products per page');
      assert(requests.at(-1).searchParams.get('pageSize') === '20', 'Server pageSize=20');
      const geometry = await page.evaluate(() => {
        const grid = document.querySelector('.catalog-grid'), bar = document.querySelector('.catalog-filter-bar');
        const fields = [...bar.querySelectorAll('input,select,button')].map(el => { const r = el.getBoundingClientRect(); return { top: r.top, bottom: r.bottom, height: r.height }; });
        return { columns: getComputedStyle(grid).gridTemplateColumns.split(' ').length, above: bar.getBoundingClientRect().bottom < grid.getBoundingClientRect().top, fields, image: document.querySelector('.book-image').getBoundingClientRect().height };
      });
      const expected = width >= 1440 ? 5 : width >= 1024 ? 4 : width >= 768 ? 3 : width < 360 ? 1 : 2;
      assert(geometry.columns === expected && geometry.above, 'Columns/filter placement ' + width);
      assert(geometry.fields.every(f => f.height === 42), 'Uniform 42px filter controls');
      if (width >= 1200) assert(geometry.fields.every(f => f.bottom === geometry.fields[0].bottom), 'Desktop single filter row');
      assert(geometry.image === (width >= 768 ? 230 : 190), 'Compact image height');
      await noOverflow();
      assert(await page.locator('.chat-launch').evaluate(el => getComputedStyle(el).position === 'static'), 'Chat does not cover product controls');
      await page.screenshot({ path: path.join(out, `catalog-${width}.png`), fullPage: true });
      if (!live) {
        assert(await page.locator('.stock-label').first().innerText() === 'Hết hàng', 'Stock badge');
        assert(await page.locator('.product-card').first().locator('.add-btn').isDisabled(), 'Stock button disabled');
        assert(await page.locator('.product-card h3').first().evaluate(el => el.clientHeight === 44 && el.scrollHeight > el.clientHeight), 'Long title clamped');
        assert((await page.locator('.catalog-toolbar').innerText()).includes('47'), 'API total, not current page count');
      }
      await page.locator('.add-btn:not([disabled])').first().click();
      assert(await page.locator('.is-added').count() === 1 && new URL(page.url()).pathname === '/products', 'Add cart without navigating');
      await page.getByRole('button', { name: 'Sau', exact: true }).click(); await page.waitForURL(/page=2/); await ready();
      assert(requests.at(-1).searchParams.get('page') === '2', 'Server page 2');
      await page.locator('#catalog-category').selectOption({ index: 1 }); await page.waitForURL(/categoryId=/); await ready();
      assert(!new URL(page.url()).searchParams.has('page'), 'Category resets page');
      await page.locator('#catalog-min').fill('70000'); await page.locator('#catalog-max').fill('180000');
      await page.getByRole('button', { name: 'Áp dụng', exact: true }).click(); await page.waitForURL(/minPrice=70000/); await ready();
      await page.locator('.sort-label select').selectOption('priceDesc'); await page.waitForURL(/sort=priceDesc/); await ready();
      const q = requests.at(-1).searchParams;
      assert(q.get('categoryId') && q.get('minPrice') === '70000' && q.get('maxPrice') === '180000' && q.get('sort') === 'priceDesc', 'Combined filters/sort');
      if (!live) {
        const prices = await page.locator('.product-bottom>strong').allTextContents();
        const values = prices.map(s => Number(s.replace(/\D/g, '')));
        assert(values.every((v, i) => i === 0 || values[i - 1] >= v), 'Descending prices');
      }
      await page.locator('.filter-chip').first().click(); await ready();
      await page.getByRole('button', { name: 'Xóa lọc', exact: true }).click(); await page.waitForURL(base + '/products'); await ready();
      assert(await page.locator('#catalog-min').inputValue() === '', 'Reset fields');
      await page.getByRole('button', { name: 'Sau', exact: true }).click(); await page.waitForURL(/page=2/); await ready();
      await page.locator('#catalog-search').fill('khong-co-sach-catalog-2026'); await page.waitForURL(/search=/); await ready();
      assert(!new URL(page.url()).searchParams.has('page'), 'Search resets page');
      await page.getByRole('heading', { name: 'Chưa tìm thấy cuốn sách phù hợp' }).waitFor(); await noOverflow();
      await page.screenshot({ path: path.join(out, `empty-${width}.png`), fullPage: true });
      await page.locator('.state button').click(); await ready();
      await page.getByRole('button', { name: 'Tư vấn chọn sách', exact: true }).click();
      await page.locator('#chat-input').waitFor();
      await page.keyboard.press('Escape');
      results.push(`PASS ${width}px: ${expected} columns; 42px filters above grid; 20/server page; combined filters/reset/chips/sort/paging/search/cart; no overflow`);
      await page.close();
    }
    assert(!errors.length, errors.join('\n'));
    results.push('PASS no browser pageerror; no server writes');
  } finally { await browser.close(); fs.writeFileSync(path.join(out, 'results.txt'), results.join('\n')); }
  console.log(results.join('\n'));
}
main().catch(e => { console.error(e); process.exitCode = 1; });