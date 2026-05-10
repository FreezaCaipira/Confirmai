import { expect, test } from "@playwright/test";

declare const process: {
  env: Record<string, string | undefined>;
};

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
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

// ─────────────────────────────────────────────────────────────────────────────
// Admin orders page (all orders)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin orders page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin order tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin orders page loads", async ({ page }) => {
    await page.goto("/admin/orders");
    await expect(page).toHaveTitle(/Pedidos|Admin|Confirmai/i);

    const hasContent = await Promise.race([
      page.locator("table, .admin-orders-wrap, .market-empty").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("admin orders page has filter inputs", async ({ page }) => {
    await page.goto("/admin/orders");
    await page.waitForLoadState("networkidle");

    const hasFilters =
      (await page.locator("input[type='text'], input[type='search'], .filter-input").count()) > 0;
    expect(hasFilters).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Admin orders-review page (pending admin release)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin orders-review page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin review tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin orders-review page loads", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await expect(page).toHaveTitle(/Revisao|Pedidos|Admin|Confirmai/i);

    const hasContent = await Promise.race([
      page.locator("table, .admin-review-wrap, .market-empty").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("admin orders-review table shows Origem column header", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const table = page.locator("table").first();
    const tableVisible = await table.isVisible().catch(() => false);

    if (!tableVisible) {
      // No orders waiting — empty state is valid
      const empty = await page.locator(".market-empty, text=nenhum, text=vazio").isVisible().catch(() => false);
      test.skip(true, "No orders in review queue — table not rendered.");
      return;
    }

    const headers = await page.locator("thead th").allTextContents();
    const hasOrigem = headers.some((h) => /origem/i.test(h));
    expect(hasOrigem).toBe(true);
  });

  test("admin orders-review release button opens modal when orders present", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator("button.success-btn.action-btn").first();
    const hasBtn = await releaseBtn.isVisible().catch(() => false);

    if (!hasBtn) {
      test.skip(true, "No orders in review queue — skipping release modal test.");
      return;
    }

    await releaseBtn.click();

    const modal = page.locator("#admin-release-funds-modal, [role='dialog'], .modal");
    await expect(modal.first()).toBeVisible({ timeout: 5_000 });
  });

  test("admin orders-review Origem badge shows Servidor or Manual", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const badges = page.locator(".badge-active, .badge-payment-info");
    const count = await badges.count();

    if (count === 0) {
      test.skip(true, "No orders in review queue — skipping badge test.");
      return;
    }

    // Each badge should say "Servidor" or "Manual"
    const texts = await badges.allTextContents();
    const allValid = texts.every((t) => /servidor|manual/i.test(t));
    expect(allValid).toBe(true);
  });

  test("admin release modal confirm button closes modal and removes order from list", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator("button.success-btn.action-btn").first();
    const hasBtn = await releaseBtn.isVisible().catch(() => false);

    if (!hasBtn) {
      test.skip(true, "No orders in review queue — skipping release confirm test.");
      return;
    }

    // Count orders before release
    const rowsBefore = await page.locator("tbody tr").count();

    // Open the modal
    await releaseBtn.click();
    const modal = page.locator("[role='dialog'], .modal-overlay, .admin-release-funds-modal").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    // Click the confirm button ("Sim, repassar")
    const confirmBtn = modal.locator("button").filter({ hasText: /Sim|repassar|confirmar/i }).first();
    await expect(confirmBtn).toBeVisible({ timeout: 5_000 });
    await confirmBtn.click();

    // Wait for Blazor to re-render after the async release
    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});

    // Modal must be gone
    await expect(modal).toBeHidden({ timeout: 10_000 });

    // Either table has one fewer row, or empty state is shown (if last order was released)
    const rowsAfter = await page.locator("tbody tr").count().catch(() => 0);
    const emptyState = await page.locator(".market-empty, .orders-review-empty").isVisible().catch(() => false);
    expect(rowsAfter < rowsBefore || emptyState).toBe(true);
  });

  test("admin release modal cancel button closes modal without changing list", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator("button.success-btn.action-btn").first();
    const hasBtn = await releaseBtn.isVisible().catch(() => false);

    if (!hasBtn) {
      test.skip(true, "No orders in review queue — skipping cancel modal test.");
      return;
    }

    const rowsBefore = await page.locator("tbody tr").count();

    await releaseBtn.click();
    const modal = page.locator("[role='dialog'], .modal-overlay, .admin-release-funds-modal").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    // Close via Escape or cancel button (if present)
    const cancelBtn = modal.locator("button").filter({ hasText: /cancelar|fechar|nao/i }).first();
    const hasCancelBtn = await cancelBtn.isVisible().catch(() => false);

    if (hasCancelBtn) {
      await cancelBtn.click();
    } else {
      await page.keyboard.press("Escape");
    }

    // Modal should close
    await expect(modal).toBeHidden({ timeout: 5_000 });

    // Row count should be unchanged
    const rowsAfter = await page.locator("tbody tr").count();
    expect(rowsAfter).toBe(rowsBefore);
  });
});
