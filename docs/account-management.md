# Account management — 2026-10-24

> Historical implementation and verification notes from before the provider conversion. The current application uses SQL Server and a new SQL Server baseline migration; MariaDB migration/test results below do not verify the current provider.

## Delivered

- `/admin/users`: server-side search (name/email), role/status filters, stable descending-ID pagination and filtered totals. Safe user projections, initial avatars, status/role badges, user detail with total orders, Completed+Paid spend, five recent orders and links to existing Admin order details. No permanent delete, Admin account creation, or editing others' personal information/passwords.
- Customer `/account/profile` and Admin `/admin/profile` reuse `ProfileView.vue`. Dedicated account menu, read-only email/role/join date, optional phone/default address, normalized name (2–120 characters), phone maximum 40 and address maximum 1000. Successful saves update Pinia/header immediately; failed saves retain input.
- Checkout initializes defaults only when created, before restoring pending checkout payload. No watch that overwrites edits. Historical orders keep their independent recipient snapshots. Checkout phone length now supports 8–40 characters instead of a narrow national-format regex.
- Separate password form with per-field visibility/native validation, confirmation mismatch, loading and duplicate guard. Policy matches registration (8–128 characters); current password verification and rejection of the existing password use the existing ASP.NET password hasher. Success clears only auth, redirects with a success notice, and preserves cart.
- Profile DTO rejects unknown properties including role/status/userId; endpoint identifies account from JWT, never arbitrary client IDs. Admin write DTOs reject unknown properties; status is required, roles restricted to Admin/Customer. Existing ProblemDetails and PageResult conventions retained.
- JWT includes `auth_version`. Every authenticated request verifies active status, stored version and stored role. Legacy JWTs without version require login. Status changes (including unlock), role changes and password changes increment version atomically. Unlock never revives an earlier token. Concurrent password requests use version/hash conditions to prevent lost updates.
- Admin status/role changes acquire MariaDB `SecurityGuards` row 1 `FOR UPDATE` in a ReadCommitted transaction; revalidate the actor after acquiring the lock; reject self changes and removing the final active Admin. The persistent database lock works across API processes. Audit rows (actor, target, action, before/after values, UTC timestamp) commit with the change. No secrets are recorded.
- Password endpoint limit: five requests per authenticated user per minute, sixth 429. No request body logging or EF sensitive-data logging added. Swagger discovers all six account/user API path patterns.

## Database

Migration `20261024073046_AccountSecurityAndUserManagement` and designer/snapshot add phone/default address, IsActive (existing accounts true), AuthVersion (1), UpdatedAt (existing CreatedAt), role/status index, SecurityGuards and UserAudits with restrictive user foreign keys.

Reviewed Up migration: additive only; no account/order/password deletion, column narrowing or reset. Generated SQL: `D:\Vibe-coding\Baitap\docs\account-security-migration.sql`. Applied successfully to the existing MariaDB via `dotnet ef database update`; no `--seed` was run and connection configuration was not changed. Existing JWTs require reauthentication after rollout.

## Files

All paths below are relative to `D:\Vibe-coding\Baitap`:

- Backend additions: `backend/BookNest.Api/Controllers/AccountController.cs`, `Controllers/UsersController.cs`, `DTOs/AccountContracts.cs`, migration and designer.
- Backend updates: `Entities/Models.cs`, `Data/BookNestDb.cs`, `DTOs/Contracts.cs`, `Controllers/AuthController.cs`, `Program.cs`, migration snapshot. Existing auth logic lives in AuthController; no separate AuthService existed.
- Frontend additions: `frontend/src/components/AccountMenu.vue`, `PasswordField.vue`, `views/account/ProfileView.vue`, `views/admin/UsersView.vue`, `UserDetailView.vue`.
- Frontend updates: `types/index.ts`, `stores/auth.ts`, `main.ts`, `router/index.ts`, `layouts/AdminLayout.vue`, `ShopLayout.vue`, `views/auth/AuthView.vue`, `views/shop/CheckoutView.vue`.
- Tests: `backend/BookNest.Tests/AccountTests.cs`, existing `BusinessTests.cs` updated for database-backed JWT validation, `scripts/account-regression.cjs`.

## Verified

- Isolated MariaDB: **26/26 passing**, including existing order/stock/catalog/review business tests and new account tests. Latest fully enabled browser run used `booknest_test_6d2ecbd82f9d4abbb3c6bfeadc22433e`; test databases retained, not dropped.
- Customer 403 on Admin APIs, own profile only, privileged-field injection 400, normalization, no historical-address changes, filtered pagination and spending totals, no secret fields in tested responses.
- Self lock/role change rejected; concurrent cross-lock/demotion of two active Admins leaves one active Admin; stale actor request rejected. Lock/login rejection, unlock not reviving old token, promotion/demotion revocation, legacy-token rejection, wrong/same/short/mismatched password rejection, password success invalidates both existing sessions and old password.
- Browser connected to real isolated API/MariaDB at port 5081, frontend at 5173. Profile save/header refresh, optional defaults, pending checkout restoration and current edits, password logout/cart preservation, user search/detail and Admin profile. Native dialog Escape cancellation and account-menu Escape. No JS page errors. At 1440/768/390px, pages do not overflow; user table scrolls independently. Reviewed mobile screenshots.
- Password rate limit verified against real API with test Admin: five wrong-password attempts 400, sixth 429. Other business tests disable rate limits intentionally.
- Controlled fixture only: profile-save 503 retaining form inputs. Dedicated auth suite uses controlled loading/401 fixtures. These are not claimed as live API successes.
- Frontend type-check/production build passed; backend compiled successfully as part of migrations/tests. Existing three xUnit2031 analyzer warnings in older BusinessTests are non-blocking.
- Swagger live JSON lists all new endpoints. Local API health returned `ok` after migration.
- Final auth regression passed at five viewport sizes; read-only storefront/Admin regression passed at 390/768/1440 with real local logins and checkout return. Catalog live GET-only and separately marked browser-fixture regressions passed at 320/390/768/1280/1536. No order, registration, catalog or account mutation was submitted to the existing application database by these regression scripts.
- Final frontend type-check and build rerun after the accessible account-menu label change also passed. Local Vite 5173 and updated API 5080 were left running for review.

Artifacts: `D:\Vibe-coding\Baitap\docs\screenshots\account-management`, `D:\Vibe-coding\Baitap\TestResults\business.trx`, `.local/account-browser-verified.log`, `.local/account-build.log`. Screenshots mask all password fields including visible modes; no tokens/credentials printed. The browser harness receives temporary test tokens only through its process environment, not files/URLs.

To rerun browser + database tests against the current provider (Vite must run at port 5173): set `BOOKNEST_BROWSER_TEST=1`, then run `.\scripts\Test-SqlServer.ps1` from the repository root. The harness creates a dedicated test database and users, starts isolated API port 5081, then terminates it. With the flag absent the optional browser test returns without launching a browser; API tests still run.

## Limitations

- Not a complete screen-reader/security audit, physical-device test, password-manager/autofill test or distributed multi-instance load test. Database serialization is exercised by concurrent requests on one test API host.
- No real-account lock, role mutation or password mutation was used for validation. No registration/order/admin mutation workflow was exercised against the existing application database; migration is the only intended schema/data-default change there.
- Test database retention consumes disk; no automatic destructive cleanup was added.