/**
 * Admin Logs & Audit Timeline E2E tests
 *
 * Covers:
 *   - AdminLogs page loads and renders filter-bar with all inputs
 *   - New EventType / EntityType filter inputs (added Mai/2026)
 *   - Applying and clearing filters
 *   - Export buttons present
 *   - Admin audit timeline page (/admin/audit/{entityType}/{entityId})
 *   - Audit timeline export buttons
 *
 * Requires: E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD
 */

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
// Admin Logs — page load & filter bar
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin Logs page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin log tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin logs page loads with heading", async ({ page }) => {
    await page.goto("/admin/logs");
    await expect(page).toHaveTitle(/Logs|Admin|Confirmai/i);
    await expect(page.locator("h1")).toBeVisible({ timeout: 10_000 });
  });

  test("filter bar contains all core inputs", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    // Global search, User, Source, Message inputs
    const inputs = page.locator(".logs-main-filter-bar input[type='text']");
    const count = await inputs.count();
    expect(count).toBeGreaterThanOrEqual(4);
  });

  test("EventType filter input is present and accepts text", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    // Find input by placeholder (PT or EN)
    const eventTypeInput = page.locator(
      "input[placeholder*='EventType' i], input[placeholder*='Tipo de evento' i], input[placeholder*='evento' i]"
    );
    await expect(eventTypeInput).toBeVisible({ timeout: 8_000 });
    await eventTypeInput.fill("user.login");
    await expect(eventTypeInput).toHaveValue("user.login");
  });

  test("EntityType filter input is present and accepts text", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const entityTypeInput = page.locator(
      "input[placeholder*='EntityType' i], input[placeholder*='Tipo de entidade' i], input[placeholder*='entidade' i]"
    );
    await expect(entityTypeInput).toBeVisible({ timeout: 8_000 });
    await entityTypeInput.fill("Order");
    await expect(entityTypeInput).toHaveValue("Order");
  });

  test("applying EventType filter shows results or empty state", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const eventTypeInput = page.locator(
      "input[placeholder*='EventType' i], input[placeholder*='Tipo de evento' i], input[placeholder*='evento' i]"
    );
    await expect(eventTypeInput).toBeVisible({ timeout: 8_000 });
    await eventTypeInput.fill("nonexistent.event.xyz");

    // Click the filter button
    const filterBtn = page.locator(".logs-main-actions button").filter({ hasText: /Filtrar|Filter/i }).first();
    await filterBtn.click();
    await page.waitForLoadState("networkidle");

    // Should show table or empty state — not an error
    const hasContent = await Promise.race([
      page.locator("table, .market-empty, .orders-review-empty, p.logs-total-count").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("clearing filters resets EventType and EntityType inputs", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const eventTypeInput = page.locator(
      "input[placeholder*='EventType' i], input[placeholder*='Tipo de evento' i], input[placeholder*='evento' i]"
    );
    await expect(eventTypeInput).toBeVisible({ timeout: 8_000 });
    await eventTypeInput.fill("some.event");

    const entityTypeInput = page.locator(
      "input[placeholder*='EntityType' i], input[placeholder*='Tipo de entidade' i], input[placeholder*='entidade' i]"
    );
    await entityTypeInput.fill("Order");

    // Click Clear
    const clearBtn = page.locator(".logs-main-actions button").filter({ hasText: /Limpar|Clear/i }).first();
    await clearBtn.click();
    await page.waitForLoadState("networkidle");

    await expect(eventTypeInput).toHaveValue("");
    await expect(entityTypeInput).toHaveValue("");
  });

  test("export buttons are present", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const csvBtn = page.locator("button.logs-export-btn").filter({ hasText: /CSV/i });
    const jsonBtn = page.locator("button.logs-export-btn").filter({ hasText: /JSON/i });

    await expect(csvBtn).toBeVisible({ timeout: 8_000 });
    await expect(jsonBtn).toBeVisible({ timeout: 8_000 });
  });

  test("quick range buttons are present", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const quickBar = page.locator(".logs-quick-range-bar");
    await expect(quickBar).toBeVisible({ timeout: 8_000 });

    const buttons = quickBar.locator("button.quick-range-btn");
    const count = await buttons.count();
    expect(count).toBeGreaterThanOrEqual(3);
  });

  test("audit quick-filter bar is present with all chips", async ({ page }) => {
    await page.goto("/admin/logs");
    await page.waitForLoadState("networkidle");

    const auditBar = page.locator(".logs-audit-filter-bar");
    await expect(auditBar).toBeVisible({ timeout: 8_000 });

    const chips = auditBar.locator("button.quick-range-btn");
    const count = await chips.count();
    expect(count).toBeGreaterThanOrEqual(4);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Admin audit timeline page
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin audit timeline", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run audit timeline tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("audit timeline route loads for Order entity", async ({ page }) => {
    await page.goto("/admin/audit/Order/1");

    const hasContent = await Promise.race([
      page.locator("h1, h2").first().waitFor({ state: "visible", timeout: 10_000 }).then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);

    // Should not show authorization error
    const unauthorized = page.getByText(/nao tem permissao|not authorized/i);
    await expect(unauthorized).toHaveCount(0);
  });

  test("audit timeline route loads for User entity", async ({ page }) => {
    await page.goto("/admin/audit/User/1");

    const hasContent = await Promise.race([
      page.locator("h1, h2").first().waitFor({ state: "visible", timeout: 10_000 }).then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("audit timeline shows event entries or empty state", async ({ page }) => {
    await page.goto("/admin/audit/Order/1");
    await page.waitForLoadState("networkidle");

    const hasEntries = await Promise.race([
      page.locator("table, .audit-timeline, .audit-entry, .market-empty").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasEntries).toBe(true);
  });

  test("audit timeline export buttons are present", async ({ page }) => {
    await page.goto("/admin/audit/Order/1");
    await page.waitForLoadState("networkidle");

    // Export buttons (CSV / JSON)
    const exportButtons = page.locator(
      "button[class*='export'], a[class*='export'], button:has-text('CSV'), button:has-text('JSON'), a:has-text('CSV'), a:has-text('JSON')"
    );
    const count = await exportButtons.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test("anonymous user is redirected from audit timeline to login", async ({ page }) => {
    await page.context().clearCookies();
    await page.goto("/admin/audit/Order/1");

    await page.waitForURL((url) => url.pathname.toLowerCase().includes("/login"), { timeout: 10_000 });
    expect(page.url().toLowerCase()).toContain("login");
  });
});
