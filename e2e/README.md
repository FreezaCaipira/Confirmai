# Confirmai E2E (Playwright)

Testes end-to-end com Playwright (TypeScript) para cenários de interação real no browser.

## Status atual

- Ultima validacao local: 2026-06-14
- Arquivos de teste: 20
- Comando: `npx playwright test --reporter=line`

## Arquivos de teste

- [tests/smoke.spec.ts](tests/smoke.spec.ts) — Smoke test basico (app responde)
- [tests/public-pages.spec.ts](tests/public-pages.spec.ts) — Paginas publicas carregam
- [tests/routes-and-meta.spec.ts](tests/routes-and-meta.spec.ts) — Rotas e meta tags
- [tests/auth-guards.spec.ts](tests/auth-guards.spec.ts) — Guards de autenticacao (login/register smoke, admin bloqueado)
- [tests/auth-forms.spec.ts](tests/auth-forms.spec.ts) — Formularios de autenticacao
- [tests/forgot-password.spec.ts](tests/forgot-password.spec.ts) — Fluxo de esqueci senha
- [tests/admin-auth-flow.spec.ts](tests/admin-auth-flow.spec.ts) — Login admin real + rotas criticas (requer env vars)
- [tests/admin-audit-logs.spec.ts](tests/admin-audit-logs.spec.ts) — Logs de auditoria admin
- [tests/admin-release-flow.spec.ts](tests/admin-release-flow.spec.ts) — Fluxo de liberacao admin (filtros, modal, release)
- [tests/admin-venues.spec.ts](tests/admin-venues.spec.ts) — Gestao de quadras admin
- [tests/cookie-consent.spec.ts](tests/cookie-consent.spec.ts) — Consentimento de cookies (aceitar/customizar/persistencia)
- [tests/language-switch.spec.ts](tests/language-switch.spec.ts) — Troca de idioma e persistencia
- [tests/events-guards.spec.ts](tests/events-guards.spec.ts) — Guards de eventos
- [tests/navigation.spec.ts](tests/navigation.spec.ts) — Navegacao geral
- [tests/mailbox.spec.ts](tests/mailbox.spec.ts) — Caixa de mensagens
- [tests/order-details.spec.ts](tests/order-details.spec.ts) — Detalhes de pedido (auth guard, estrutura, badges, release)
- [tests/purchase-flow.spec.ts](tests/purchase-flow.spec.ts) — Fluxo de compra (marketplace, unauthenticated, my-orders)
- [tests/purchase-flow-full.spec.ts](tests/purchase-flow-full.spec.ts) — Fluxo completo ponta-a-ponta (pagamento → pedido → admin libera → finalizado)
- [tests/server-listing.spec.ts](tests/server-listing.spec.ts) — Listagem de servidores
- [tests/server-manage.spec.ts](tests/server-manage.spec.ts) — Gestao de servidores

## Testes que requerem env vars

- Cenarios admin requerem `E2E_ADMIN_EMAIL` e `E2E_ADMIN_PASSWORD`
- Se nao definidos, Playwright marca esses testes como skipped
- `purchase-flow-full.spec.ts` requer servidor em modo Development (endpoint `/api/test/seed-order`)

## Prerequisites

- Node.js 20+ e npm
- Confirmai app rodando localmente

## Install

```bash
cd e2e
npm install
npm run install:browsers
```

## Run

```bash
# Inicia o app automaticamente via dotnet run e executa E2E
npm test

# Ou defina URL customizada
set E2E_BASE_URL=http://127.0.0.1:5001
npm test
```

Admin flow (requer env vars):

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

## Se app ja estiver rodando

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

## Rodar testes especificos

```bash
# Arquivo unico
npx playwright test tests/auth-guards.spec.ts

# Por nome (grep)
npx playwright test --grep "cookie consent"
```

## Troubleshooting

- Se todos os testes falham com `ERR_CONNECTION_REFUSED`, rode sem `E2E_SKIP_WEBSERVER` ou inicie o app manualmente antes.
- Em localhost/dev, logica de reset de cookie-consent pode rodar no reload; testes evitam depender exclusivamente de visibilidade do banner pos-reload.
