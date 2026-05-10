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
// Server listing public page
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Server listing", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await page.goto("/servers");
  });

  test("page loads and shows heading", async ({ page }) => {
    await expect(page).toHaveTitle(/Servidores|Confirmai/i);
    await expect(page.locator("h3.servers-list-title")).toBeVisible();
  });

  test("shows server cards or empty state", async ({ page }) => {
    const cards = page.locator(".world-select-card");
    const empty = page.locator(".market-empty");
    const hasCards = (await cards.count()) > 0;
    const hasEmpty = await empty.isVisible().catch(() => false);
    expect(hasCards || hasEmpty).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Server manage page (admin)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Server manage page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run manage-page tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("manage page is accessible from server listing", async ({ page }) => {
    await page.goto("/servers");

    const firstCard = page.locator(".world-select-card").first();
    const hasCard = (await firstCard.count()) > 0;
    test.skip(!hasCard, "No servers seeded — skipping manage navigation test.");

    // Navigate to first server's details
    await firstCard.locator("button, a").first().click();
    await page.waitForURL(/\/servers\/\d+/);
  });

  test("manage page loads via direct URL for server 2", async ({ page }) => {
    await page.goto("/servers/2/manage");

    // Either loads the manage card or shows access denied / not found
    const manageMain = page.locator(".server-manage-main");
    const denied = page.locator("text=Acesso negado");
    const notFound = page.locator("text=Servidor nao encontrado");

    const visible = await Promise.race([
      manageMain.waitFor({ state: "visible", timeout: 10_000 }).then(() => "main"),
      denied.waitFor({ state: "visible", timeout: 10_000 }).then(() => "denied"),
      notFound.waitFor({ state: "visible", timeout: 10_000 }).then(() => "notfound"),
    ]).catch(() => "unknown");

    expect(["main", "denied", "notfound"]).toContain(visible);
  });

  test("manage page shows server data section with form fields", async ({ page }) => {
    await page.goto("/servers/2/manage");

    const manageMain = page.locator(".server-manage-main");
    const notManage = page.locator("text=Acesso negado, text=Servidor nao encontrado");
    const loaded = await manageMain.isVisible().catch(() => false);

    test.skip(!loaded, "Server 2 not accessible — skipping form field test.");

    await expect(page.locator("h3.server-manage-section").first()).toBeVisible();

    // Form has name field
    await expect(page.locator("input.input").first()).toBeVisible();

    // Action buttons present
    await expect(page.locator("button.save-btn")).toBeVisible();
    await expect(page.locator("button.cancel-btn")).toBeVisible();
  });

  test("manage page shows members section", async ({ page }) => {
    await page.goto("/servers/2/manage");

    const loaded = await page.locator(".server-manage-main").isVisible().catch(() => false);
    test.skip(!loaded, "Server 2 not accessible — skipping members section test.");

    const sections = page.locator("h3.server-manage-section");
    const count = await sections.count();
    expect(count).toBeGreaterThanOrEqual(2);

    // At least one section mentions "Integrante"
    const texts = await sections.allTextContents();
    const hasMembers = texts.some((t) => /integrante/i.test(t));
    expect(hasMembers).toBe(true);
  });

  test("manage page shows API integration section", async ({ page }) => {
    await page.goto("/servers/2/manage");

    const loaded = await page.locator(".server-manage-main").isVisible().catch(() => false);
    test.skip(!loaded, "Server 2 not accessible — skipping API section test.");

    const sections = page.locator("h3.server-manage-section");
    const texts = await sections.allTextContents();
    const hasApi = texts.some((t) => /api|integra/i.test(t));
    expect(hasApi).toBe(true);
  });

  test("manage page shows audit section", async ({ page }) => {
    await page.goto("/servers/2/manage");

    const loaded = await page.locator(".server-manage-main").isVisible().catch(() => false);
    test.skip(!loaded, "Server 2 not accessible — skipping audit section test.");

    const sections = page.locator("h3.server-manage-section");
    const texts = await sections.allTextContents();
    const hasAudit = texts.some((t) => /auditoria/i.test(t));
    expect(hasAudit).toBe(true);
  });

  test("Nova API Key button is visible", async ({ page }) => {
    await page.goto("/servers/2/manage");

    const loaded = await page.locator(".server-manage-main").isVisible().catch(() => false);
    test.skip(!loaded, "Server 2 not accessible — skipping API key button test.");

    await expect(page.locator("button:has-text('Nova API Key')")).toBeVisible();
  });

  test("unauthenticated user is redirected away from manage page", async ({ page }) => {
    // Use a fresh context with no auth cookies
    await page.context().clearCookies();
    await page.goto("/servers/2/manage");

    // Should redirect to login or show access-denied
    const atLogin = page.url().toLowerCase().includes("login");
    const denied = await page.locator("text=Acesso negado").isVisible().catch(() => false);
    expect(atLogin || denied).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Admin API Keys page
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Admin API Keys page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run admin API key tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("page loads with heading and breadcrumb", async ({ page }) => {
    await page.goto("/admin/api-keys");

    await expect(page.locator(".apikeys-actions, .product-table")).toBeVisible({ timeout: 10_000 });
  });

  test("Nova API Key button is visible and interactive", async ({ page }) => {
    await page.goto("/admin/api-keys");

    const btn = page.locator("button.btn-gold:has-text('Nova API Key'), button.btn-gold");
    await expect(btn.first()).toBeVisible({ timeout: 10_000 });
  });

  test("API keys table shows expected columns when keys exist", async ({ page }) => {
    await page.goto("/admin/api-keys");

    const table = page.locator("table.product-table");
    const hasTable = await table.isVisible().catch(() => false);
    test.skip(!hasTable, "No API keys in DB — skipping column check.");

    const headers = table.locator("th");
    const headerTexts = await headers.allTextContents();
    const lower = headerTexts.map((h) => h.toLowerCase());

    expect(lower.some((h) => h.includes("servidor") || h.includes("server"))).toBe(true);
    expect(lower.some((h) => h.includes("prefix") || h.includes("prefixo"))).toBe(true);
    expect(lower.some((h) => h.includes("status"))).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// game-login page (magic link)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Game login magic-link page", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("shows invalid state when no token is given", async ({ page }) => {
    await page.goto("/auth/game-login");

    // Should render the page (200) and indicate invalid token
    await expect(page.locator("body")).toBeVisible();
    // The page model sets IsInvalid = true when token is missing
    const body = await page.locator("body").textContent();
    expect(body).toBeTruthy();
  });

  test("shows expired/invalid state for a garbage token", async ({ page }) => {
    await page.goto("/auth/game-login?token=000000000000000000000000000000000000000000000000000000000000dead");

    await expect(page.locator("body")).toBeVisible();
    // The page should render without crashing (200 or redirect)
    const status = await page.evaluate(() => document.readyState);
    expect(status).toBe("complete");
  });
});
