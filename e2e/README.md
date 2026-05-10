# Confirmai E2E (Playwright)

This folder contains browser end-to-end tests for real UI interaction scenarios that are not covered by server-side integration tests.

## Coverage mapping and status

- Last local validation: 2025-07-10
- Result: 10 passed, 0 failed, 2 skipped (full suite with admin env vars passes all)
- Command: npx playwright test --reporter=line

Current test files and covered scenarios:

- [tests/auth-guards.spec.ts](tests/auth-guards.spec.ts)
	- Login page smoke (core fields)
	- Register page smoke (core fields)
	- Anonymous user blocked on admin route
- [tests/admin-auth-flow.spec.ts](tests/admin-auth-flow.spec.ts)
	- Seeded admin real login flow
	- Access smoke for critical admin routes
	- Note: this file contains scenarios that are skipped when admin env vars are missing
- [tests/cookie-consent.spec.ts](tests/cookie-consent.spec.ts)
	- Accept all
	- Reject optional
	- Customize + save
	- Cancel without persistence
	- localStorage/cookie persistence contracts
- [tests/language-switch.spec.ts](tests/language-switch.spec.ts)
	- Language switch through flag click
	- Persistence after navigation
	- Persistence after full refresh
- [tests/order-details.spec.ts](tests/order-details.spec.ts)
	- Auth guard (redirect unauthenticated, authenticated loads)
	- Page structure (breadcrumb, stepper, chat, order-id, grid)
	- AguardandoRevisaoAdm status (admin notice, release button, modal, cancel)
	- Release flow (success message, stepper update, cross-page count)
- [tests/admin-release-flow.spec.ts](tests/admin-release-flow.spec.ts)
	- Admin orders page loads with filters
	- Orders-review table structure and Origem badge
	- Release modal opens, confirms, and removes order from queue
	- Cancel modal without changing list
- [tests/purchase-flow.spec.ts](tests/purchase-flow.spec.ts)
	- Marketplace page loads
	- Unauthenticated buy redirect
	- My-orders page load
	- Order details for order 1 (not-found is valid)
	- Docs/integration page and Lua download links
	- Offer form page access
- [tests/purchase-flow-full.spec.ts](tests/purchase-flow-full.spec.ts) ← **full ponta-a-ponta**
	- Pedido aparece em /orders após pagamento confirmado (seed via `POST /api/test/seed-order`)
	- /orders mostra status AguardandoEntrega para o pedido confirmado
	- Detalhes acessíveis em /orders/{id} após pagamento
	- Badge de status AguardandoEntrega em detalhes do pedido
	- Pedido AguardandoRevisaoAdm aparece em /admin/orders-review
	- Admin libera pedido específico → sai da fila de revisão
	- Após liberação, pedido mostra status Finalizado em /orders/{id}
	- Stepper marca etapa final como concluída após liberação

Why some tests are skipped:

- The seeded-admin scenarios require E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD.
- If these variables are not defined, Playwright marks those tests as skipped by design.
- The purchase-flow-full.spec.ts suite also requires a server running in Development mode (for the `/api/test/seed-order` endpoint).

## Covered scenarios

- Cookie consent interactions (accept/customize/persistence)
- Language switch by flag click and persistence on protected routes
- Identity authentication pages smoke checks (login/register)
- Anonymous access guard on admin routes
- Seeded admin real login flow and critical admin route smoke
- Order details page (auth guard, structure, status badges, release flow)
- Admin orders-review table (structure, Origem badge, release modal, cancel modal)
- **Full purchase flow ponta-a-ponta**: pagamento confirmado → pedido em /orders → admin libera → status Finalizado (`purchase-flow-full.spec.ts`)

## Prerequisites

- Node.js 20+ and npm
- Confirmai app running locally

## Install

```bash
cd e2e
npm install
npm run install:browsers
```

## Run

```bash
# Starts the app automatically via dotnet run and executes E2E
npm test

# Or set a custom URL
set E2E_BASE_URL=http://127.0.0.1:5001
npm test
```

Seeded admin flow (requires env vars):

```bash
# Git Bash
E2E_ADMIN_EMAIL="your-admin@email" E2E_ADMIN_PASSWORD="your-password" npm test -- --grep "Seeded admin"
```

```powershell
# PowerShell
$env:E2E_ADMIN_EMAIL="your-admin@email"
$env:E2E_ADMIN_PASSWORD="your-password"
npm test -- --grep "Seeded admin"
```

Windows PowerShell alternative:

```powershell
$env:E2E_BASE_URL="http://127.0.0.1:5001"
npm test
```

## If app is already running

```bash
# Git Bash
E2E_SKIP_WEBSERVER=1 npm test

# PowerShell
$env:E2E_SKIP_WEBSERVER="1"; npm test
```

## HTML report

```bash
npx playwright show-report
```

## Run only specific tests

```bash
# Run a single file
npx playwright test tests/auth-guards.spec.ts

# Run by test name (grep)
npx playwright test --grep "cookie consent"
```

## Practical troubleshooting

- If all tests fail with `ERR_CONNECTION_REFUSED`, run without `E2E_SKIP_WEBSERVER` or start the app manually first.
- On localhost/dev, cookie-consent reset logic can run on reload; tests avoid asserting consent persistence exclusively through post-reload banner visibility.
- If a single test flakes, rerun once with `npm test -- --grep "<test name>"` before changing app code.
- Seeded-admin tests auto-skip when `E2E_ADMIN_EMAIL`/`E2E_ADMIN_PASSWORD` are not set.

## Notes

- These tests are intentionally separated from the .NET test project.
- They validate browser/runtime behaviors such as JavaScript click handlers and localStorage/cookies.


