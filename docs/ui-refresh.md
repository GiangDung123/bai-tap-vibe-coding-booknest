# BookNest UI refresh

## Scope

Frontend-only update. Vue 3, TypeScript, Tailwind **v4**, Pinia, Vue Router, Lucide and Chart.js remain unchanged. No dependency was added. API contracts, authorization, stock/order/payment rules, database and local secrets were not modified.

## Visual system

- Paper `#F8F7F4`, white surfaces, ink `#182230`, muted `#667085`, brand `#2457D6`, hover `#1D46AD`, selection `#EEF3FF`, border `#E5E7EB`.
- Tailwind v4 `@theme` tokens in `D:\Vibe-coding\Baitap\frontend\src\assets\main.css` define colors, fonts, radius, section spacing and a soft shadow.
- Local system fonts: Segoe UI / Arial for interface; Times New Roman / Times for editorial headings. No external font requests or new font-license requirements.
- Shared Button, Input, Select, Badge, SectionHeading, BookCover, ProductCard, Skeleton, FormField validation, Drawer, ConfirmDialog, EmptyState and Pagination.
- Demo `cover-1.png` through `cover-5.png` are represented by typography using the API's real title/author/category with a visible “BÌA MINH HỌA” label. Uploaded cover images remain images, displayed with `object-fit: contain`. Database image URLs are unchanged.
- Mobile header is intentionally **not sticky** to avoid covering focused form fields. Desktop header is sticky.
- Chat launcher is in normal document flow on mobile purchase/checkout screens so it cannot obscure a purchase CTA. Chat remains manually opened, never simulates an AI response.

## Updated screens

Home, catalog/filtering, product detail, cart, checkout, login/register, personal orders, order detail/payment, admin shell/dashboard and all existing management tables/forms through shared styling, chatbot, empty/error/loading/confirmation states.

## Validation

Run from `D:\Vibe-coding\Baitap\frontend`:

```powershell
npm run type-check
npm run build
```

Read-only browser suite (requires the existing ignored Playwright install, installed Chrome and running frontend/backend):

```powershell
node D:\Vibe-coding\Baitap\scripts\ui-regression.cjs
```

Screenshots and results: `D:\Vibe-coding\Baitap\docs\screenshots\ui-refresh`.

Coverage at 390, 768 and 1440 CSS px: homepage/catalog/detail/cart/login/checkout/orders, admin dashboard/products/categories/orders/product-create form; real API data; search with empty results and reset, category filter/chip removal, add-to-cart without navigation, quantity change, confirmation cancel, successful customer/admin login, checkout redirect, password toggle, inline required-field validation, drawer Escape and focus restoration, chat opening/empty-send state, page errors and document horizontal overflow.

The suite creates **no orders**, submits no admin data and does not modify stock. Cart edits are confined to isolated browser storage. Login uses ignored local seed credentials without printing them.

## Catalog horizontal filters — October 24, 2026

- Replaced the left sidebar/drawer with one responsive filter form above the full-width catalog. Desktop order: search, category, minimum price, maximum price, apply, reset. Controls measure 42px high; the form uses 16px padding, a 12px radius and 14px gaps. Mobile search/category each occupy a full row; prices and actions use pairs. No decorative sidebar text remains.
- Catalog-only container expands to 1440px without changing the shared header/home container. Grid columns: 5 at >=1440px, 4 at >=1024px, 3 at >=768px, 2 below 768px, 1 below 360px.
- `ProductCard` has an opt-in `compact` prop; homepage cards retain their original layout. Catalog images measure 230px on desktop and 190px on mobile, with contained covers; titles are limited to two lines, authors to one, and cart buttons remain 44px square. Grid gaps are 20px desktop / 14px mobile, with 28px between rows.
- API still owns filtering, sorting and pagination. Catalog requests explicitly send `pageSize=20` (the existing backend accepts 1–50), and result totals use `totalItems`. Filter/search/sort changes reset pagination and preserve the existing query/filter-chip behavior. Reset also clears unapplied form values.
- The chat launcher is in normal document flow only on the catalog to avoid covering full-width product cards. Opening/closing chat remains unchanged.

### Verified results

- `npm run type-check` and `npm run build`: passed after final UI edits.
- `node D:\Vibe-coding\Baitap\scripts\catalog-regression.cjs`: passed against the **live local API**, at 390/768/1280/1536/320px; confirmed 2/3/4/5/1 columns, 42px controls, filter position, image heights, 20 items per server page, combined category/price/sort parameters, reset/chip removal, pagination, search-to-page-1, empty results, cart addition without navigation, chat open/Escape, and no document overflow or page errors.
- API initially reported a database connection error during service startup but recovered before the live browser run. Live results are recorded separately from fixtures; no backend/configuration/database edits were made.
- `node D:\Vibe-coding\Baitap\scripts\catalog-regression.cjs --fixtures`: passed the same widths with browser-only fixtures, additionally asserting a deliberately long title's two-line clamp, out-of-stock badge/disabled button, API total distinct from page item count, and descending prices. Fixture data is never stored on the server.
- `node D:\Vibe-coding\Baitap\scripts\ui-regression.cjs`: passed customer flows at 390/768/1440px and read-only Admin screens at 1440/768/390px. Updated the test selector to use the new always-visible filter form.
- Reviewed generated screenshots, including mobile/tablet catalog, large desktop catalog, long-title fixture and empty-result mobile state.
- Screenshots/results: `D:\Vibe-coding\Baitap\docs\screenshots\catalog-horizontal`; explicit fixtures are in its `fixtures` subdirectory. Existing broader-suite screenshots remain in `D:\Vibe-coding\Baitap\docs\screenshots\ui-refresh`.
- Tests create no orders or admin/catalog writes. Cart changes are isolated browser storage. Live AI, physical-device testing, mutation workflows and a full accessibility audit remain outside this validation.

## Auth card refresh — October 24, 2026

- Added `D:\Vibe-coding\Baitap\frontend\src\layouts\AuthLayout.vue`, selected by `authLayout` route metadata for `/login` and `/register` in App.vue. Storefront/Admin layouts, auth guards, auth store and API contracts are unchanged. Auth screens contain only the logo/home link header, content and a small signature; no shop search/menu or chatbot is mounted.
- Login uses one 1040px card with a 48% navy gradient illustration and 52% white form, 24px corners and soft shadow. Desktop min-height is 610px, not a fixed height. Below 860px it becomes a single-column form with a short brand line. The page uses a #F7F8FC base, flexible vertical centering and normal document scrolling.
- Three decorative HTML/CSS books require no product API, images or external resources. They are hidden from assistive technology. No fabricated customer/review statistics or testimonials were added.
- Inputs use existing Lucide icons, 16px text, 50px height, username/current-password autocomplete, native inline FormField validation and scoped autofill/focus styles. Password visibility preserves value/selection; keyboard toggling remains possible. Form submission has an in-handler duplicate guard, disabled button, animated spinner and specific loading text. Existing session-expired/system errors, password clearing after submission, safe redirects and cart storage behavior remain.
- Registration shares the same visual shell and keeps all original fields and endpoint; no registration was submitted during validation.

### Auth validation results

- Final `npm run type-check` and `npm run build`: passed.
- `node D:\Vibe-coding\Baitap\scripts\auth-regression.cjs`: passed at 1440x1000, 768x1024, 390x844, 1440x500 and 390x420. Confirmed layout switching/no overflow, absence of shop header/chat on auth, no catalog requests, 16px inputs/autocomplete attributes, keyboard skip link and visible focus, required errors below fields, password selection/value/focus preservation, keyboard toggle, Enter submission, loading spinner/disabled submit, duplicate-request blocking, error presentation, registration link preserving checkout redirect, and reachable submit on short screens. Login failure/loading use an intercepted 401 fixture, not real invalid credentials.
- Calculated contrast for principal text colors: body/white 16.03:1, muted and placeholder/white 4.97:1, white/brand button 6.16:1, description/lightest navy endpoint 6.58:1, heading/lightest navy endpoint 8.12:1. This is not a complete accessibility audit.
- `node D:\Vibe-coding\Baitap\scripts\ui-regression.cjs`: passed with real local customer/Admin login, checkout redirect and preserved cart; customer screens at 390/768/1440px and read-only Admin screens at 1440/768/390px. The screenshot helper now masks password fields, including when visibility is toggled. No passwords/tokens are logged.
- Reviewed login screenshots for desktop/tablet/mobile and low desktop height. Screenshots/results: `D:\Vibe-coding\Baitap\docs\screenshots\auth-refresh`. Dedicated auth screenshots only capture empty login/register forms.
- No database/account/password/configuration changes, new orders or Admin/catalog writes. Browser cart changes are isolated to test contexts.
- Not verified: physical mobile keyboards/iOS/Android behavior, actual third-party password manager/autofill interaction, screen-reader audit, registration creation, live invalid-credential response and live Gemini. Short viewport scrolling and controlled error/loading behavior were verified instead.

## Previously recorded general limitations

## Account/profile extension — 2026-10-24

Implemented live Admin user management, shared Customer/Admin profiles, password change, JWT revocation and additive MariaDB migration. Detailed file inventory, concurrency/security design, migration application and test scope are recorded in `D:\Vibe-coding\Baitap\docs\account-management.md`. Evidence: `D:\Vibe-coding\Baitap\docs\screenshots\account-management`. API and browser mutation tests used dedicated test databases/accounts only. Earlier statements that auth contracts/database were unchanged describe the previous UI-only milestone, not this extension.

- Payment success/failure, cancellation, reviews and admin saves are not re-executed by this UI-only suite; they mutate data. Their existing API calls and business conditions are retained.
- Live Gemini output/configuration is not verified by this suite; no API key was read or changed for the UI work.
- Tested with desktop Chrome at responsive widths, not physical iOS/Android devices or a full screen-reader audit.
- Native date inputs follow the browser locale; business timestamps continue using the existing Vietnam timezone formatter.