/**
 * Product form E2E tests
 *
 * Covers:
 *  - Auth guards for /products/create and /admin/items
 *  - Admin product list page (/admin/items)
 *  - Product create form (/products/create)
 *  - Product create → submit → redirect to /products
 *  - Admin product edit form (/admin/items/edit/{id})
 *
 * Requires E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD env vars for authenticated tests.
 */

import { expect, Page, test } from "@playwright/test";

declare const process: {
  env: Record<string, string | undefined>;
};

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

async function dismissCookieBanner(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem(
      "Confirmai.cookieConsent.v1",
      JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString(),
      })
    );
  });
}

async function loginAsAdmin(page: Page) {
  await page.goto("/Identity/Account/Login");
  await page.locator("input[name='Input.Email']").fill(adminEmail ?? "");
  await page.locator("input[name='Input.Password']").fill(adminPassword ?? "");
  const submit = page.locator("form button[type='submit']");
  await expect(submit).toBeVisible();
  await Promise.all([
    page.waitForURL((url) => !url.pathname.toLowerCase().includes("/login"), {
      timeout: 15_000,
    }),
    submit.click(),
  ]);
}

// ─────────────────────────────────────────────────────────────────────────────
// Auth guards
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Product form — auth guards", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("unauthenticated user is redirected from /products/create to login", async ({ page }) => {
    await page.goto("/products/create");
    const url = page.url();
    const isLoginOrAccessDenied =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account") ||
      (await page
        .locator("text=Acesso negado, text=login")
        .isVisible()
        .catch(() => false));
    expect(isLoginOrAccessDenied).toBe(true);
  });

  test("unauthenticated user is redirected from /admin/items to login", async ({ page }) => {
    await page.goto("/admin/items");
    const url = page.url();
    const isLoginOrAccessDenied =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account") ||
      (await page
        .locator("text=Acesso negado, text=login")
        .isVisible()
        .catch(() => false));
    expect(isLoginOrAccessDenied).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Admin products list page
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin products list — /admin/items", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin product tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin products page loads with title", async ({ page }) => {
    await page.goto("/admin/items");
    await expect(page).toHaveTitle(/Itens|Admin|Confirmai/i);

    const pageLoaded = await page
      .locator(".admin-items-page, .entity-shell")
      .waitFor({ state: "visible", timeout: 10_000 })
      .then(() => true)
      .catch(() => false);

    expect(pageLoaded).toBe(true);
  });

  test("admin products page has name filter input", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".filter-bar").waitFor({ state: "visible", timeout: 10_000 });
    const filterInput = page.locator(".filter-bar input[type='text']");
    await expect(filterInput).toBeVisible();
  });

  test("admin products page has New Item button that links to /products/create", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".filter-bar").waitFor({ state: "visible", timeout: 10_000 });

    const newItemBtn = page.locator("button.success-btn");
    await expect(newItemBtn).toBeVisible();

    await Promise.all([
      page.waitForURL((url) => url.pathname === "/products/create", { timeout: 10_000 }),
      newItemBtn.click(),
    ]);

    expect(page.url()).toContain("/products/create");
  });

  test("admin products table shows headers or empty state", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".admin-items-page").waitFor({ state: "visible", timeout: 10_000 });

    // Wait for loading to complete (AdminDataState hides content while loading)
    await page
      .locator(".loading-spinner, .admin-data-state-loading")
      .waitFor({ state: "hidden", timeout: 10_000 })
      .catch(() => {});

    const hasTable = (await page.locator("table.product-table").count()) > 0;
    const hasEmpty = await page
      .locator(".market-empty, .admin-data-state-empty")
      .isVisible()
      .catch(() => false);

    expect(hasTable || hasEmpty).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Product create form — /products/create
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Product create form — /products/create", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run product form tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("product form loads with required fields", async ({ page }) => {
    await page.goto("/products/create");
    await page.locator(".product-form, .entity-shell").waitFor({ state: "visible", timeout: 10_000 });

    // Name input (required field)
    await expect(page.locator("input.input").first()).toBeVisible();

    // Category select
    await expect(page.locator("select.input").first()).toBeVisible();

    // Submit button
    await expect(page.locator("button.pf-submit-btn")).toBeVisible();

    // Cancel button
    await expect(page.locator("button.pf-cancel-btn")).toBeVisible();
  });

  test("cancel button navigates back to /products", async ({ page }) => {
    await page.goto("/products/create");
    await page.locator("button.pf-cancel-btn").waitFor({ state: "visible", timeout: 10_000 });

    await Promise.all([
      page.waitForURL((url) => url.pathname === "/products", { timeout: 10_000 }),
      page.locator("button.pf-cancel-btn").click(),
    ]);

    expect(page.url()).toContain("/products");
  });

  test("submitting without name shows validation error", async ({ page }) => {
    await page.goto("/products/create");
    await page.locator("button.pf-submit-btn").waitFor({ state: "visible", timeout: 10_000 });

    // Clear name field if it has a default value and submit
    const nameInput = page.locator("input.input").first();
    await nameInput.clear();
    await page.locator("button.pf-submit-btn").click();

    // Blazor validation renders either a ValidationSummary or inline ValidationMessage
    const hasValidation = await Promise.race([
      page
        .locator(".validation-message, .validation-errors, [role='alert']")
        .first()
        .waitFor({ state: "visible", timeout: 5_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasValidation).toBe(true);
    // Should still be on the create page
    expect(page.url()).toContain("/products/create");
  });

  test("valid product can be created and redirects to /products", async ({ page }) => {
    await page.goto("/products/create");
    await page.locator(".product-form, .entity-shell").waitFor({ state: "visible", timeout: 10_000 });

    const uniqueName = `E2E Test Item ${Date.now()}`;

    // Fill the name field (first text input inside the form)
    const nameInput = page.locator("input.input").first();
    await nameInput.fill(uniqueName);

    // Submit the form
    await Promise.all([
      page.waitForURL((url) => url.pathname !== "/products/create", { timeout: 20_000 }),
      page.locator("button.pf-submit-btn").click(),
    ]);

    // After successful creation, navigates to /products
    expect(page.url()).toMatch(/\/products(\/|$|\?)/);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Admin product edit form — /admin/items/edit/{id}
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin product edit form — /admin/items/edit/{id}", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin edit tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("admin products list shows edit buttons when items exist", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".admin-items-page").waitFor({ state: "visible", timeout: 10_000 });
    await page
      .locator(".loading-spinner, .admin-data-state-loading")
      .waitFor({ state: "hidden", timeout: 10_000 })
      .catch(() => {});

    const editButtons = page.locator("button.edit-btn, a.edit-btn");
    const count = await editButtons.count();

    // If no products exist, skip the rest of edit tests
    test.skip(count === 0, "No products in database — skipping edit button assertions.");

    await expect(editButtons.first()).toBeVisible();
  });

  test("clicking edit navigates to admin edit form with pre-filled name", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".admin-items-page").waitFor({ state: "visible", timeout: 10_000 });
    await page
      .locator(".loading-spinner, .admin-data-state-loading")
      .waitFor({ state: "hidden", timeout: 10_000 })
      .catch(() => {});

    const editButtons = page.locator("button.edit-btn, a.edit-btn");
    const count = await editButtons.count();
    test.skip(count === 0, "No products in database — skipping edit navigation test.");

    await Promise.all([
      page.waitForURL((url) => /\/admin\/items\/edit\/\d+|\/admin\/products\/edit\/\d+/.test(url.pathname), {
        timeout: 10_000,
      }),
      editButtons.first().click(),
    ]);

    // Admin edit page has name input with existing product name
    const nameInput = page.locator("input.input").first();
    await expect(nameInput).toBeVisible();
    const nameValue = await nameInput.inputValue();
    expect(nameValue.trim().length).toBeGreaterThan(0);
  });

  test("admin edit form has save and cancel buttons", async ({ page }) => {
    await page.goto("/admin/items");
    await page.locator(".admin-items-page").waitFor({ state: "visible", timeout: 10_000 });
    await page
      .locator(".loading-spinner, .admin-data-state-loading")
      .waitFor({ state: "hidden", timeout: 10_000 })
      .catch(() => {});

    const editButtons = page.locator("button.edit-btn, a.edit-btn");
    const count = await editButtons.count();
    test.skip(count === 0, "No products in database — skipping admin edit form assertions.");

    await Promise.all([
      page.waitForURL((url) => /\/admin\/items\/edit\/\d+|\/admin\/products\/edit\/\d+/.test(url.pathname), {
        timeout: 10_000,
      }),
      editButtons.first().click(),
    ]);

    await page.locator(".entity-shell, form").waitFor({ state: "visible", timeout: 10_000 });

    // Save button
    const saveBtn = page.locator("button[type='submit'].save-btn, button.save-btn, button[type='submit']").first();
    await expect(saveBtn).toBeVisible();

    // Cancel button
    const cancelBtn = page.locator("button.cancel-btn").first();
    await expect(cancelBtn).toBeVisible();
  });
});
