// UI-only failure/loading fixtures. No credentials, request bodies or tokens are logged.
const { chromium } = require('../.local/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const out = path.resolve(__dirname, '../docs/screenshots/auth-refresh');
const base = 'http://localhost:5173';
function assert(value, message) { if (!value) throw new Error(message); }
function contrast(a, b) {
  const luminance = hex => {
    const rgb = hex.match(/[a-f\d]{2}/gi).map(v => parseInt(v, 16) / 255).map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4);
    return rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722;
  };
  const x = luminance(a), y = luminance(b);
  return (Math.max(x, y) + .05) / (Math.min(x, y) + .05);
}
async function main() {
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: true });
  const results = [], errors = [];
  try {
    for (const [name, text, background] of [['body', '182230', 'ffffff'], ['muted/placeholder', '667085', 'ffffff'], ['button/link', 'ffffff', '2457d6'], ['navy description', 'd7e4fa', '244d87'], ['navy heading', 'fffaf0', '244d87']]) {
      const ratio = contrast(text, background);
      assert(ratio >= 4.5, 'Text contrast ' + name);
      results.push(`PASS text contrast ${name}: ${ratio.toFixed(2)}:1`);
    }
    for (const [width, height] of [[1440, 1000], [768, 1024], [390, 844], [1440, 500], [390, 420]]) {
      const page = await browser.newPage({ viewport: { width, height } });
      page.on('pageerror', () => errors.push('Browser page error'));
      let submissions = 0, catalogRequests = 0;
      page.on('request', r => { if (/\/api\/products/.test(r.url())) catalogRequests++; });
      await page.route('**/api/auth/login', async route => {
        submissions++;
        await new Promise(resolve => setTimeout(resolve, 900));
        await route.fulfill({ status: 401, json: { title: 'Email hoặc mật khẩu không chính xác.' } });
      });
      await page.goto(base + '/login?redirect=/checkout');
      await page.locator('#email').waitFor();
      assert(await page.locator('.site-header,.header-search,.chat-launch').count() === 0, 'No shop header/search/chat on auth');
      assert(catalogRequests === 0, 'Illustration independent of catalog API');
      assert(await page.locator('#email').getAttribute('autocomplete') === 'username', 'Username autofill');
      assert(await page.locator('#password').getAttribute('autocomplete') === 'current-password', 'Password autofill');
      assert(await page.locator('#email').evaluate(el => getComputedStyle(el).fontSize === '16px'), '16px input');
      assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No horizontal overflow');
      const columns = await page.locator('.auth-card').evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length);
      assert(columns === (width >= 860 ? 2 : 1), 'Responsive card columns');
      await page.screenshot({ path: path.join(out, `login-${width}-${height}.png`), fullPage: true });
      await page.keyboard.press('Tab');
      assert(await page.locator('.skip-link').evaluate(el => el === document.activeElement), 'Keyboard skip link first');
      await page.locator('#email').focus();
      assert(await page.locator('#email').evaluate(el => getComputedStyle(el).boxShadow !== 'none'), 'Visible input focus');
      await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
      await page.locator('#email[aria-invalid=true]').waitFor();
      assert(await page.locator('#email-error').isVisible(), 'Inline required error');
      await page.locator('#email').fill('ui-only@example.invalid');
      await page.locator('#password').fill('Ui-only-not-a-real-password');
      await page.locator('#password').evaluate(el => { el.focus(); el.setSelectionRange(3, 7); });
      await page.getByRole('button', { name: 'Hiện mật khẩu' }).click();
      assert(await page.locator('#password').evaluate(el => el.type === 'text' && el.selectionStart === 3 && el.selectionEnd === 7 && el.value.length === 27 && document.activeElement === el), 'Toggle keeps value/caret/focus');
      await page.getByRole('button', { name: 'Ẩn mật khẩu' }).focus();
      await page.keyboard.press('Enter');
      assert(await page.locator('#password').getAttribute('type') === 'password', 'Keyboard password toggle');
      await page.locator('#password').press('Enter');
      const pending = page.getByRole('button', { name: 'Đang đăng nhập…', exact: true });
      await pending.waitFor();
      assert(await pending.isDisabled(), 'Loading disables submit');
      assert(await page.locator('.auth-spinner').isVisible(), 'Loading spinner');
      await page.locator('form').evaluate(el => { el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })); });
      await page.getByRole('alert').filter({ hasText: 'Email hoặc mật khẩu không chính xác.' }).waitFor();
      assert(submissions === 1, 'Duplicate submit blocked');
      assert(await page.locator('#password').inputValue() === '', 'Password cleared after failure');
      await page.locator('.auth-submit').scrollIntoViewIfNeeded();
      assert(await page.locator('.auth-submit').evaluate(el => { const r = el.getBoundingClientRect(); return r.top >= 0 && r.bottom <= innerHeight; }), 'Submit reachable on short screen');
      await page.getByRole('link', { name: 'Đăng ký ngay', exact: true }).click();
      await page.waitForURL('**/register?redirect=/checkout');
      await page.locator('#confirm').waitFor();
      assert(await page.locator('#password').getAttribute('autocomplete') === 'new-password', 'Registration preserved');
      await page.screenshot({ path: path.join(out, `register-${width}-${height}.png`), fullPage: true });
      await page.getByRole('link', { name: 'Đăng nhập', exact: true }).click();
      await page.waitForURL('**/login?redirect=/checkout');
      await page.getByRole('link', { name: 'Về trang chủ', exact: true }).click();
      await page.waitForURL(base + '/');
      await page.locator('.site-header').waitFor();
      assert(await page.locator('.chat-launch').count() === 1, 'Shop header/chat restored outside auth');
      results.push(`PASS ${width}x${height}: layout, empty-form screenshot, keyboard/focus, inline validation, password toggle/caret, Enter/loading/duplicate guard, fixture error, registration redirect, short-screen scroll, shop header/chat`);
      await page.close();
    }
    assert(errors.length === 0, 'No page errors');
    results.push('PASS no page errors; failure/loading requests intercepted; no real login attempted by this script; no server mutations; no sensitive screenshots/logs');
  } finally { await browser.close(); fs.writeFileSync(path.join(out, 'results.txt'), results.join('\n')); }
  console.log(results.join('\n'));
}
main().catch(() => { console.error('Auth regression failed. Inspect assertions locally; no request data logged.'); process.exitCode = 1; });