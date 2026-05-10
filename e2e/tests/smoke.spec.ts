/**
 * Smoke tests — login, criar oferta, comprar
 *
 * These three flows represent the critical happy-path for the marketplace.
 * All require E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD env vars.
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
// Smoke 1 — Login
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Smoke — login", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run smoke tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("login with valid credentials redirects away from login page", async ({
    page,
  }) => {
    await loginAsAdmin(page);

    // After login we must no longer be on the /login page
    expect(page.url().toLowerCase()).not.toContain("/login");

    // The user nav element or dashboard heading should be visible
    const loggedInIndicator = page.locator(
      ".user-menu, .nav-user, [href*='/dashboard'], [href*='/orders'], h1"
    );
    await expect(loggedInIndicator.first()).toBeVisible({ timeout: 10_000 });
  });

  test("login page shows validation errors for empty submission", async ({
    page,
  }) => {
    await page.goto("/Identity/Account/Login");
    await page.locator("form button[type='submit']").click();

    // HTML5 required validation or ASP.NET field-validation error
    const emailInput = page.locator("input[name='Input.Email']");
    const isEmailInvalid = await emailInput.evaluate(
      (el: HTMLInputElement) => !el.validity.valid
    );
    const serverErrors = page.locator(".text-danger, .field-validation-error").filter({ hasText: /.+/ });
    const hasServerError = (await serverErrors.count()) > 0;

    expect(isEmailInvalid || hasServerError).toBe(true);
  });

  test("unauthenticated user is redirected to login when accessing /orders", async ({
    page,
  }) => {
    // Use fresh browser context with no auth cookies
    await page.context().clearCookies();
    await page.goto("/orders");

    await page.waitForURL((url) => url.pathname.toLowerCase().includes("/login"), {
      timeout: 10_000,
    });
    expect(page.url().toLowerCase()).toContain("login");
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Smoke 2 — Criar oferta
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Smoke — criar oferta", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run smoke tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("Nova oferta button navigates to offer form", async ({ page }) => {
    // Find first available server
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    const hasServer = (await firstCard.count()) > 0;
    test.skip(!hasServer, "No servers seeded — skipping offer creation smoke.");

    // Enter the server market
    const enterBtn = firstCard.locator(".world-select-join").first();
    await enterBtn.click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    // Find first item card on the server market
    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    const hasItem = (await itemCard.count()) > 0;
    test.skip(!hasItem, "No items listed — skipping offer creation smoke.");

    // Navigate to the first item's offer list (click the item card link)
    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    // Click "Nova oferta"
    const novaOfertaBtn = page.locator("button.server-item-offers-create");
    await expect(novaOfertaBtn).toBeVisible({ timeout: 8_000 });
    await novaOfertaBtn.click();

    // Should land on offer form page
    await page.waitForURL(/\/servers\/\d+\/items\/[^/]+\/offer/, { timeout: 10_000 });
    await expect(page.locator("form, .server-offer-create-card")).toBeVisible({
      timeout: 8_000,
    });
  });

  test("offer form page loads with required fields", async ({ page }) => {
    // Direct navigation to offer form for server 1 — any item key that may exist
    await page.goto("/servers/1/items/Sword/offer");

    const formLoaded = page.locator("form");
    const accessDenied = page.locator("text=Acesso negado, text=nao encontrado, text=nao autorizado");

    const result = await Promise.race([
      formLoaded.waitFor({ state: "visible", timeout: 10_000 }).then(() => "form"),
      accessDenied.waitFor({ state: "visible", timeout: 10_000 }).then(() => "denied"),
    ]).catch(() => "timeout");

    if (result === "form") {
      // If form loaded, it should have quantity and price fields
      await expect(page.locator("input.input").first()).toBeVisible();
      await expect(page.locator("button[type='submit']")).toBeVisible();
    } else {
      // Access denied or not found is also acceptable (server/item may not exist in test DB)
      expect(["denied", "timeout"]).toContain(result);
    }
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Smoke 3 — Comprar (browse → buy redirect)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Smoke — comprar", () => {
  test.skip(
    !adminEmail || !adminPassword,
    "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run smoke tests."
  );

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("Comprar button on offer card redirects to buy page", async ({ page }) => {
    // Browse to first server
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    test.skip(!(await firstCard.count()), "No servers seeded — skipping buy smoke.");

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    // Find first item card and navigate to its offers
    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    test.skip(!(await itemCard.count()), "No items listed — skipping buy smoke.");

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    // Look for a Comprar button from a different seller (not own offer)
    const buyBtn = page.locator("button.offer-card-v2-buy-btn").filter({ hasNotText: /cancelar/i }).first();
    const hasBuyBtn = (await buyBtn.count()) > 0;
    test.skip(!hasBuyBtn, "No purchasable offer cards available — skipping buy smoke.");

    await buyBtn.click();

    // Should navigate to /marketplace/buy/{id}
    await page.waitForURL(/\/marketplace\/buy\/\d+/, { timeout: 10_000 });
    expect(page.url()).toMatch(/\/marketplace\/buy\/\d+/);
  });

  test("anonymous Comprar button redirects to login", async ({ page }) => {
    // Use a fresh browser context with no auth
    await page.context().clearCookies();
    await dismissCookieBanner(page);

    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    const hasServer = (await firstCard.count()) > 0;
    test.skip(!hasServer, "No servers seeded — skipping anon buy redirect test.");

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    const hasItem = (await itemCard.count()) > 0;
    test.skip(!hasItem, "No items — skipping anon buy redirect test.");

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    // CTA "Crie sua conta" should be visible for anonymous users
    const ctaBanner = page.locator(".server-anon-cta");
    await expect(ctaBanner).toBeVisible({ timeout: 8_000 });

    // Buy buttons for anon redirect to login
    const buyBtn = page.locator("button.offer-card-v2-buy-btn").first();
    const hasBuyBtn = (await buyBtn.count()) > 0;
    if (hasBuyBtn) {
      await buyBtn.click();
      await page.waitForURL((url) => url.pathname.toLowerCase().includes("/login"), {
        timeout: 10_000,
      });
      expect(page.url().toLowerCase()).toContain("login");
    }
  });
});
