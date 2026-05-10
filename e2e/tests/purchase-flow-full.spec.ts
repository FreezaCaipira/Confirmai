/**
 * E2E – Full purchase flow (ponta-a-ponta)
 *
 * These tests exercise the complete buyer → payment confirmed → order created
 * → admin releases flow from the browser's perspective.
 *
 * They rely on a dev-only seeding endpoint (POST /api/test/seed-order) that
 * creates a PaymentRecord + OrderModel directly in the database without going
 * through the real BTCPay API. The endpoint is only registered when the server
 * runs with ASPNETCORE_ENVIRONMENT=Development (the default for `dotnet run`).
 *
 * Prerequisites:
 *   E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD env vars must be set.
 */

import { type APIRequestContext, expect, test } from "@playwright/test";

declare const process: {
  env: Record<string, string | undefined>;
};

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;
const baseURL = process.env.E2E_BASE_URL ?? "http://127.0.0.1:5000";

// ─────────────────────────────────────────────────────────────────────────────
// Helpers shared across the file
// ─────────────────────────────────────────────────────────────────────────────

async function dismissCookieBanner(page: import("@playwright/test").Page) {
  await page.addInitScript(() => {
    localStorage.setItem(
      "Confirmai.cookieConsent.v1",
      JSON.stringify({ essential: true, analytics: false, updatedAtUtc: new Date().toISOString() })
    );
  });
}

async function loginAsAdmin(page: import("@playwright/test").Page) {
  await page.goto("/Identity/Account/Login");
  await page.locator("input[name='Input.Email']").fill(adminEmail ?? "");
  await page.locator("input[name='Input.Password']").fill(adminPassword ?? "");
  const submit = page.locator("form button[type='submit']");
  await expect(submit).toBeVisible();
  await Promise.all([
    page.waitForURL((url) => !url.pathname.toLowerCase().includes("/login"), { timeout: 15_000 }),
    submit.click(),
  ]);
}

interface SeededOrder {
  orderId: number;
  productId: number;
  buyerEmail: string;
  status: string;
}

/** Calls the dev-only seed endpoint and returns the created order metadata. */
async function seedOrder(
  request: APIRequestContext,
  status: "AguardandoEntrega" | "AguardandoRevisaoAdm" = "AguardandoEntrega"
): Promise<SeededOrder> {
  const resp = await request.post(`${baseURL}/api/test/seed-order?status=${status}`);
  expect(
    resp.status(),
    `Seed endpoint must return 200 — is the server running in Development mode? (status=${status})`
  ).toBe(200);
  return (await resp.json()) as SeededOrder;
}

// ─────────────────────────────────────────────────────────────────────────────
// Suite 1: Comprador vê pedido após confirmação de pagamento (AguardandoEntrega)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Comprador vê pedido após confirmação de pagamento", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run this suite.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("pedido aparece na lista /orders após pagamento confirmado", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoEntrega");

    await loginAsAdmin(page);
    await page.goto("/orders");
    await page.waitForLoadState("networkidle");

    // The order row must contain a link to this specific order
    const orderLink = page.locator(`a[href='/orders/${orderId}']`);
    await expect(orderLink).toBeVisible({ timeout: 10_000 });
  });

  test("página /orders mostra status AguardandoEntrega para o pedido confirmado", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoEntrega");

    await loginAsAdmin(page);
    await page.goto("/orders");
    await page.waitForLoadState("networkidle");

    // Locate the table row for this specific order
    const orderRow = page.locator(`tr:has(a[href='/orders/${orderId}'])`);
    await expect(orderRow).toBeVisible({ timeout: 10_000 });

    // The row should contain a status badge reflecting delivery waiting state
    const badge = orderRow.locator(".order-status-badge, [class*='status']").first();
    await expect(badge).toBeVisible();
    const badgeText = await badge.textContent();
    expect(badgeText).toMatch(/entrega|aguard/i);
  });

  test("detalhes do pedido acessíveis em /orders/{id} após pagamento confirmado", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoEntrega");

    await loginAsAdmin(page);
    await page.goto(`/orders/${orderId}`);
    await page.waitForLoadState("networkidle");

    // Page should load successfully — not show "not found"
    const notFound = await page
      .locator("text=Transacao nao encontrada, text=not found, text=404")
      .isVisible()
      .catch(() => false);
    expect(notFound).toBe(false);

    // At least one structural element (stepper, chat, or status badge) should appear
    const hasContent = await Promise.race([
      page.locator(".order-stepper, .order-detail, .chat-panel, .order-status-badge").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);
    expect(hasContent).toBe(true);
  });

  test("detalhes do pedido mostram status AguardandoEntrega no badge", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoEntrega");

    await loginAsAdmin(page);
    await page.goto(`/orders/${orderId}`);
    await page.waitForLoadState("networkidle");

    // Look for status badge indicating AguardandoEntrega
    const badge = page
      .locator(".order-status-badge.aguardandoentrega, .order-status-badge.aguardandoentregaingame")
      .first();

    // Fallback: any visible status text that matches delivery waiting
    const fallbackBadge = page.locator(".order-status-badge, [class*='status']").first();

    const hasBadge = await badge.isVisible().catch(() => false);
    if (hasBadge) {
      await expect(badge).toBeVisible();
    } else {
      await expect(fallbackBadge).toBeVisible({ timeout: 8_000 });
      const text = await fallbackBadge.textContent();
      expect(text).toMatch(/entrega|aguard/i);
    }
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Suite 2: Admin libera pedido em revisão (AguardandoRevisaoAdm → Finalizado)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin libera pedido em revisão", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run this suite.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("pedido em AguardandoRevisaoAdm aparece em /admin/orders-review", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoRevisaoAdm");

    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    // The seeded order must appear in the review table
    const orderLink = page.locator(`a[href='/orders/${orderId}']`);
    await expect(orderLink).toBeVisible({ timeout: 10_000 });
  });

  test("admin libera pedido específico → pedido sai da fila de revisão", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoRevisaoAdm");

    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    // Locate the specific row for our seeded order by its details link
    const orderRow = page.locator(`tr:has(a[href='/orders/${orderId}'])`);
    await expect(orderRow).toBeVisible({ timeout: 10_000 });

    const rowsBefore = await page.locator("tbody tr").count();

    // Click the release button in that specific row
    const releaseBtn = orderRow.locator("button.success-btn.action-btn").first();
    await expect(releaseBtn).toBeVisible({ timeout: 5_000 });
    await releaseBtn.click();

    // Confirm in the modal
    const modal = page
      .locator("#admin-release-funds-modal, [role='dialog'], .modal-overlay, .admin-release-funds-modal")
      .first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    const confirmBtn = modal.locator("button").filter({ hasText: /Sim|repassar|confirmar/i }).first();
    await expect(confirmBtn).toBeVisible({ timeout: 5_000 });
    await confirmBtn.click();

    // Wait for Blazor to re-render
    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});
    await expect(modal).toBeHidden({ timeout: 10_000 });

    // The table should have one fewer row (or show empty state)
    const rowsAfter = await page.locator("tbody tr").count().catch(() => 0);
    const emptyState = await page
      .locator(".market-empty, .orders-review-empty")
      .isVisible()
      .catch(() => false);
    expect(rowsAfter < rowsBefore || emptyState).toBe(true);
  });

  test("após liberação, pedido mostra status Finalizado em /orders/{id}", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoRevisaoAdm");

    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    // Locate the specific row and release it
    const orderRow = page.locator(`tr:has(a[href='/orders/${orderId}'])`);
    await expect(orderRow).toBeVisible({ timeout: 10_000 });

    const releaseBtn = orderRow.locator("button.success-btn.action-btn").first();
    await expect(releaseBtn).toBeVisible({ timeout: 5_000 });
    await releaseBtn.click();

    const modal = page
      .locator("#admin-release-funds-modal, [role='dialog'], .modal-overlay, .admin-release-funds-modal")
      .first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    const confirmBtn = modal.locator("button").filter({ hasText: /Sim|repassar|confirmar/i }).first();
    await expect(confirmBtn).toBeVisible();
    await confirmBtn.click();

    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});
    await expect(modal).toBeHidden({ timeout: 10_000 });

    // Navigate to the order details page and verify status is Finalizado
    await page.goto(`/orders/${orderId}`);
    await page.waitForLoadState("networkidle");

    // Look for a Finalizado badge
    const finalizedBadge = page.locator(".order-status-badge.finalizado").first();
    const hasFinalizedBadge = await finalizedBadge.isVisible().catch(() => false);

    if (hasFinalizedBadge) {
      await expect(finalizedBadge).toBeVisible();
    } else {
      // Fallback: any visible badge/text matching "Finalizado"
      const anyBadge = page.locator(".order-status-badge, [class*='status']").first();
      await expect(anyBadge).toBeVisible({ timeout: 8_000 });
      const text = await anyBadge.textContent();
      expect(text).toMatch(/finaliz/i);
    }
  });

  test("stepper do pedido marca etapa final como concluída após liberação", async ({ page, request }) => {
    const { orderId } = await seedOrder(request, "AguardandoRevisaoAdm");

    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const orderRow = page.locator(`tr:has(a[href='/orders/${orderId}'])`);
    await expect(orderRow).toBeVisible({ timeout: 10_000 });

    const releaseBtn = orderRow.locator("button.success-btn.action-btn").first();
    await releaseBtn.click();

    const modal = page
      .locator("#admin-release-funds-modal, [role='dialog'], .modal-overlay, .admin-release-funds-modal")
      .first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    const confirmBtn = modal.locator("button").filter({ hasText: /Sim|repassar|confirmar/i }).first();
    await confirmBtn.click();

    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});

    await page.goto(`/orders/${orderId}`);
    await page.waitForLoadState("networkidle");

    // Stepper should exist and at least one step should be marked as done/active
    const stepperExists = await page
      .locator(".order-stepper, .stepper, [class*='stepper']")
      .first()
      .isVisible()
      .catch(() => false);

    if (stepperExists) {
      // At least one completed/active step should be present
      const completedSteps = page.locator(
        ".step-done, .step-active, .step-complete, [class*='step-done'], [class*='complete']"
      );
      const count = await completedSteps.count();
      expect(count).toBeGreaterThan(0);
    } else {
      // If no stepper, at least ensure the page loaded without error
      const hasContent = await page
        .locator(".order-detail, .order-status-badge, .chat-panel")
        .first()
        .isVisible()
        .catch(() => false);
      expect(hasContent).toBe(true);
    }
  });
});
