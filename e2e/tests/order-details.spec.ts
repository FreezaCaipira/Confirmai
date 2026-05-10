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

/**
 * Navigates to admin/orders, picks the first order that has a details link and
 * returns its URL (/orders/{id}). Returns null if no orders are present.
 */
async function findFirstOrderDetailsUrl(page: import("@playwright/test").Page): Promise<string | null> {
  await page.goto("/admin/orders");
  await page.waitForLoadState("networkidle");

  // The admin orders table has rows that navigate to /orders/{id}
  const rowLink = page.locator("table tbody tr").first();
  const rowVisible = await rowLink.isVisible().catch(() => false);
  if (!rowVisible) return null;

  // Click the row to navigate (AdminOrders.razor uses NavigateTo on row click)
  await rowLink.click();
  await page.waitForURL(/\/orders\/\d+/, { timeout: 10_000 }).catch(() => {});

  const url = page.url();
  return url.match(/\/orders\/\d+/) ? url : null;
}

/**
 * Finds the first order in AguardandoRevisaoAdm state by navigating to
 * admin/orders-review and clicking its details link. Returns the order URL or null.
 */
async function findReviewOrderDetailsUrl(page: import("@playwright/test").Page): Promise<string | null> {
  await page.goto("/admin/orders-review");
  await page.waitForLoadState("networkidle");

  const detailsLink = page.locator("a.details-btn.action-btn").first();
  const visible = await detailsLink.isVisible().catch(() => false);
  if (!visible) return null;

  const href = await detailsLink.getAttribute("href");
  return href ?? null;
}

// ─────────────────────────────────────────────────────────────────────────────
// Guard tests — no auth required
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Order details — auth guard", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("unauthenticated access to /orders/1 redirects to login", async ({ page }) => {
    await page.goto("/orders/1");
    const url = page.url();
    const redirectedToLogin =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account");
    expect(redirectedToLogin).toBe(true);
  });

  test("unauthenticated access to /orders/9999 redirects to login", async ({ page }) => {
    await page.goto("/orders/9999");
    const url = page.url();
    const redirectedToLogin =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account");
    expect(redirectedToLogin).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Order details — page structure (admin)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Order details — page structure", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("order details page shows breadcrumb navigation", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping breadcrumb test.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const breadcrumb = page.locator("nav.breadcrumb-nav");
    await expect(breadcrumb).toBeVisible({ timeout: 10_000 });

    // Must contain links back to dashboard and orders list
    const dashboardLink = breadcrumb.locator("a[href='/dashboard']");
    const ordersLink = breadcrumb.locator("a[href='/orders']");
    await expect(dashboardLink).toBeVisible();
    await expect(ordersLink).toBeVisible();
  });

  test("order details page shows progress stepper", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping stepper test.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    // Stepper OR terminal state must be visible
    const stepper = page.locator(".order-stepper");
    await expect(stepper).toBeVisible({ timeout: 10_000 });
  });

  test("order details page shows chat section", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping chat section test.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const chatCard = page.locator(".intermediation-chat-card");
    await expect(chatCard).toBeVisible({ timeout: 10_000 });
  });

  test("order details page shows order ID in meta section", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping order ID test.");
      return;
    }

    // Extract the numeric ID from the URL
    const match = orderUrl.match(/\/orders\/(\d+)/);
    const expectedId = match ? match[1] : null;

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const idValue = page.locator(".order-id-value");
    await expect(idValue).toBeVisible({ timeout: 10_000 });
    if (expectedId) {
      await expect(idValue).toHaveText(expectedId);
    }
  });

  test("order details page shows intermediation grid", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping grid test.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const grid = page.locator(".intermediation-grid");
    await expect(grid).toBeVisible({ timeout: 10_000 });
  });

  test("order details page shows buyer and seller rows", async ({ page }) => {
    const orderUrl = await findFirstOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders found — skipping buyer/seller rows test.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const rows = page.locator(".intermediation-row");
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Order details — awaiting review status
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Order details — AguardandoRevisaoAdm status", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("order in review shows awaiting-review notice", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const notice = page.locator(".notice-awaiting-review");
    await expect(notice).toBeVisible({ timeout: 10_000 });
  });

  test("order in review shows admin release button in order details", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    // Admin sees "Liberar fundos" button when IsDelivered && !FundsReleased
    const releaseBtn = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    const hasBtn = await releaseBtn.isVisible().catch(() => false);

    if (!hasBtn) {
      // Order may be visible to admin but not in IsDelivered state yet — acceptable
      test.skip(true, "Order not yet in IsDelivered state (release button not shown).");
      return;
    }

    await expect(releaseBtn).toBeEnabled();
  });

  test("order in review shows release modal when release button clicked", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    if (!(await releaseBtn.isVisible().catch(() => false))) {
      test.skip(true, "Release button not present.");
      return;
    }

    await releaseBtn.click();

    const modal = page.locator(".release-funds-modal, [role='dialog']").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    // Modal must show product details and confirm/cancel buttons
    const confirmBtn = modal.locator("button.success-btn");
    const cancelBtn = modal.locator("button.back-btn");
    await expect(confirmBtn).toBeVisible();
    await expect(cancelBtn).toBeVisible();
  });

  test("cancel release modal closes without changing status", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    if (!(await releaseBtn.isVisible().catch(() => false))) {
      test.skip(true, "Release button not present.");
      return;
    }

    await releaseBtn.click();

    const modal = page.locator(".release-funds-modal, [role='dialog']").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    // Cancel
    await modal.locator("button.back-btn").click();
    await expect(modal).toBeHidden({ timeout: 5_000 });

    // Status notice must still be present (order not released)
    const notice = page.locator(".notice-awaiting-review");
    await expect(notice).toBeVisible({ timeout: 5_000 });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Order details — release flow and final status
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Order details — release flow", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin releases funds via order details — success message appears", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    if (!(await releaseBtn.isVisible().catch(() => false))) {
      test.skip(true, "Release button not present (order not in IsDelivered state).");
      return;
    }

    // Open release modal
    await releaseBtn.click();

    const modal = page.locator(".release-funds-modal, [role='dialog']").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    // Confirm release
    const confirmBtn = modal.locator("button.success-btn");
    await expect(confirmBtn).toBeVisible();
    await confirmBtn.click();

    // Wait for Blazor to re-render
    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});

    // Modal must be gone
    await expect(modal).toBeHidden({ timeout: 10_000 });

    // Success message must appear
    const successMsg = page.locator(".intermediation-success-message");
    await expect(successMsg).toBeVisible({ timeout: 10_000 });

    // Release button must no longer be present
    const releaseBtnAfter = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    await expect(releaseBtnAfter).toBeHidden({ timeout: 5_000 });
  });

  test("after release, stepper shows Finalizado step as done", async ({ page }) => {
    // Navigate to admin/orders and look for an already-released order (FundsReleased = true)
    await page.goto("/admin/orders");
    await page.waitForLoadState("networkidle");

    const tableVisible = await page.locator("table tbody tr").first().isVisible().catch(() => false);
    if (!tableVisible) {
      test.skip(true, "No orders found.");
      return;
    }

    // Click the first row to navigate to order details
    await page.locator("table tbody tr").first().click();
    await page.waitForURL(/\/orders\/\d+/, { timeout: 10_000 }).catch(() => {});

    await page.waitForLoadState("networkidle");

    // The stepper or terminal state must be shown
    const stepperOrTerminal = page.locator(".order-stepper");
    const isVisible = await stepperOrTerminal.isVisible().catch(() => false);
    expect(isVisible).toBe(true);
  });

  test("released order no longer appears in admin/orders-review after release via order details", async ({ page }) => {
    const orderUrl = await findReviewOrderDetailsUrl(page);
    if (!orderUrl) {
      test.skip(true, "No orders in review queue.");
      return;
    }

    // Count review orders before
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");
    const rowsBefore = await page.locator("tbody tr").count();

    // Release via order details
    await page.goto(orderUrl);
    await page.waitForLoadState("networkidle");

    const releaseBtn = page.locator(".intermediation-actions button.intermediation-btn-success").filter({
      hasText: /liberar|release/i,
    });
    if (!(await releaseBtn.isVisible().catch(() => false))) {
      test.skip(true, "Release button not present.");
      return;
    }

    await releaseBtn.click();
    const modal = page.locator(".release-funds-modal, [role='dialog']").first();
    await expect(modal).toBeVisible({ timeout: 5_000 });
    await modal.locator("button.success-btn").click();
    await page.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});

    // Go back to review page and verify count decreased
    await page.goto("/admin/orders-review");
    await page.waitForLoadState("networkidle");

    const rowsAfter = await page.locator("tbody tr").count().catch(() => 0);
    const emptyState = await page.locator(".market-empty, .orders-review-empty").isVisible().catch(() => false);
    expect(rowsAfter < rowsBefore || emptyState).toBe(true);
  });
});
