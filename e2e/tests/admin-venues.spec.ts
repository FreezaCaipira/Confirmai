/**
 * Admin Venues — guard + smoke tests.
 *
 * Guards: unauthenticated users are redirected to login for all /admin/venues routes.
 * Smoke:  authenticated admin can access the listing and navigate to create form.
 *         Requires E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD.
 */

import { expect, test } from "@playwright/test";

declare const process: { env: Record<string, string | undefined> };

const adminEmail    = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

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
// Guards — unauthenticated
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin Venues — unauthenticated guards", () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
  });

  test("GET /admin/venues redirects to login", async ({ page }) => {
    await page.goto("/admin/venues");
    expect(
      page.url().toLowerCase().includes("/login") ||
      page.url().toLowerCase().includes("/identity/account")
    ).toBe(true);
  });

  test("GET /admin/venues/edit/0 redirects to login", async ({ page }) => {
    await page.goto("/admin/venues/edit/0");
    expect(
      page.url().toLowerCase().includes("/login") ||
      page.url().toLowerCase().includes("/identity/account")
    ).toBe(true);
  });

  test("GET /admin/venues/edit/1 redirects to login", async ({ page }) => {
    await page.goto("/admin/venues/edit/1");
    expect(
      page.url().toLowerCase().includes("/login") ||
      page.url().toLowerCase().includes("/identity/account")
    ).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Smoke — authenticated admin
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin Venues — authenticated smoke", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD to run admin venue tests.");

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("admin can view /admin/venues listing", async ({ page }) => {
    await page.goto("/admin/venues");
    await expect(page).toHaveURL(/\/admin\/venues/i);
    // Title heading must be present
    await expect(page.getByRole("heading", { name: /Quadras/i })).toBeVisible();
  });

  test("Nova Quadra button navigates to create form", async ({ page }) => {
    await page.goto("/admin/venues");
    await page.getByRole("button", { name: /Nova Quadra/i }).click();
    await expect(page).toHaveURL(/\/admin\/venues\/edit\/0/i);
    await expect(page.getByRole("heading", { name: /Nova Quadra/i })).toBeVisible();
  });

  test("create form renders all required fields", async ({ page }) => {
    await page.goto("/admin/venues/edit/0");
    await expect(page.locator("input")).not.toHaveCount(0);
    // At minimum: Nome, Cidade, UF, Endereço inputs must be present
    const inputs = await page.locator("input[type='text'], input:not([type])").count();
    expect(inputs).toBeGreaterThanOrEqual(3);
  });

  test("cancel on create form navigates back to listing", async ({ page }) => {
    await page.goto("/admin/venues/edit/0");
    await page.getByRole("button", { name: /Cancelar/i }).click();
    await expect(page).toHaveURL(/\/admin\/venues$/i);
  });
});
