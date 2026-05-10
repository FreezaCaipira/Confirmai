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
// Marketplace public page
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Marketplace", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("marketplace page loads and shows heading or empty state", async ({ page }) => {
    await page.goto("/marketplace");
    await expect(page).toHaveTitle(/Marketplace|Confirmai/i);

    const hasContent = await Promise.race([
      page.locator("h1, h2, h3").first().waitFor({ state: "visible", timeout: 8_000 }).then(() => true),
      page.locator(".market-empty").waitFor({ state: "visible", timeout: 8_000 }).then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("buy page redirects unauthenticated user to login", async ({ page }) => {
    await page.goto("/marketplace/buy/1");
    const url = page.url();
    // Should redirect to login or show access denied
    const isLoginOrDenied =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account") ||
      (await page.locator("text=Acesso negado, text=login").isVisible().catch(() => false));
    expect(isLoginOrDenied).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// My orders page (authenticated)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("My orders page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run authenticated order tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("my orders page loads", async ({ page }) => {
    await page.goto("/orders");
    await expect(page).toHaveTitle(/Pedidos|Confirmai/i);

    // Either shows orders list or empty state
    const hasContent = await Promise.race([
      page.locator("table, .orders-list, .market-empty, h1, h2, h3").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("order details page returns valid response for order 1", async ({ page }) => {
    await page.goto("/orders/1");

    // Page loads — either shows order details or not-found message
    const hasContent = await Promise.race([
      page.locator(".order-details-wrap, .order-card, .market-empty, .not-found").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
      page.locator("text=Pedido nao encontrado, text=nao encontrado").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Docs / integration page (smoke)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Docs integration page", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run docs/integration tests (requires ServerAdmin role).");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("docs/integration page loads and shows content", async ({ page }) => {
    await page.goto("/docs/integration");

    await expect(page).toHaveTitle(/Integra|Confirmai/i);

    const hasHeading = await page.locator("h1, h2, h3").first()
      .waitFor({ state: "visible", timeout: 8_000 })
      .then(() => true)
      .catch(() => false);

    expect(hasHeading).toBe(true);
  });

  test("docs/integration page shows download links or integration details", async ({ page }) => {
    await page.goto("/docs/integration");

    await page.waitForLoadState("networkidle");

    // Page should contain some download-related element or integration instructions
    const hasDownload =
      (await page.locator("a[href*='download'], a[href*='.lua'], a[href*='.zip'], a[href*='script']").count()) > 0;
    const hasInstructions =
      (await page.locator("pre, code, .code-block, .integration-instructions").count()) > 0;
    const hasContent =
      (await page.locator("p").count()) > 2;

    expect(hasDownload || hasInstructions || hasContent).toBe(true);
  });

  test("Lua download links are reachable (status 200)", async ({ page, request }) => {
    await page.goto("/docs/integration");
    await page.waitForLoadState("networkidle");

    const luaLinks = page.locator("a[href*='.lua'], a[href*='download'][href*='lua' i]");
    const count = await luaLinks.count();
    if (count === 0) {
      test.skip(true, "No .lua download links found on docs/integration page.");
      return;
    }

    const hrefs: string[] = [];
    for (let i = 0; i < count; i++) {
      const href = await luaLinks.nth(i).getAttribute("href");
      if (href) hrefs.push(href);
    }

    const baseUrl = new URL(page.url()).origin;
    for (const href of hrefs) {
      const url = href.startsWith("http") ? href : `${baseUrl}${href}`;
      const response = await request.get(url);
      expect(response.status(), `Expected 200 for ${url}`).toBe(200);
    }
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Server offer creation page (authenticated)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Server offer form", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run offer form tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("offer form page for a known server and item is accessible", async ({ page }) => {
    await page.goto("/servers/1/items/Sword/offer");

    // Loads form, access denied, or not found
    const visible = await Promise.race([
      page.locator("form, .offer-form, .server-offer-form").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => "form"),
      page.locator("text=Acesso negado, text=nao encontrado, text=inativo").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => "denied"),
    ]).catch(() => "timeout");

    expect(["form", "denied"]).toContain(visible);
  });

  test("offer form allows filling quantity and price fields", async ({ page }) => {
    await page.goto("/servers/1/items/Sword/offer");
    await page.waitForLoadState("networkidle");

    const form = page.locator("form").first();
    const formVisible = await form.isVisible().catch(() => false);
    if (!formVisible) {
      test.skip(true, "Offer form not rendered — server or item may not exist in this environment.");
      return;
    }

    // Fill quantity input
    const quantityInput = form.locator("input[type='number']").nth(0);
    await expect(quantityInput).toBeVisible({ timeout: 8_000 });
    await quantityInput.fill("10");
    await expect(quantityInput).toHaveValue("10");

    // Fill unit price input
    const priceInput = form.locator("input[type='number']").nth(1);
    await expect(priceInput).toBeVisible({ timeout: 8_000 });
    await priceInput.fill("50");
    await expect(priceInput).toHaveValue("50");

    // Submit button should be enabled
    const submitBtn = form.locator("button[type='submit']");
    await expect(submitBtn).toBeEnabled({ timeout: 5_000 });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Payment page content (authenticated)
// Tests the /marketplace/buy/{id} page render after navigating from offer list.
// Gracefully skips when no seeded server/item/offer exists.
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Payment page content", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run payment page tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("payment page renders product summary after buy-button click", async ({ page }) => {
    // Navigate through: servers → server market → item offers → click buy
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    if (!(await firstCard.count())) {
      test.skip(true, "No servers seeded — skipping payment page content test.");
      return;
    }

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    if (!(await itemCard.count())) {
      test.skip(true, "No items listed — skipping payment page content test.");
      return;
    }

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    const buyBtn = page.locator("button.offer-card-v2-buy-btn").filter({ hasNotText: /cancelar/i }).first();
    if (!(await buyBtn.count())) {
      test.skip(true, "No purchasable offer — skipping payment page content test.");
      return;
    }

    await buyBtn.click();
    await page.waitForURL(/\/marketplace\/buy\/\d+/, { timeout: 10_000 });

    // Payment page must render the product summary shell
    await expect(page.locator(".tibia-pay-shell, .payment-container")).toBeVisible({ timeout: 10_000 });

    // Product summary card with item rows should be present
    const summaryCard = page.locator(".tibia-pay-card-product, .tibia-pay-item-card");
    await expect(summaryCard.first()).toBeVisible({ timeout: 8_000 });

    // At least one label/value row should be visible
    const rows = page.locator(".tibia-pay-item-row, .tibia-pay-value");
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test("payment page shows seller information", async ({ page }) => {
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    if (!(await firstCard.count())) {
      test.skip(true, "No servers seeded.");
      return;
    }

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    if (!(await itemCard.count())) {
      test.skip(true, "No items listed.");
      return;
    }

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    const buyBtn = page.locator("button.offer-card-v2-buy-btn").filter({ hasNotText: /cancelar/i }).first();
    if (!(await buyBtn.count())) {
      test.skip(true, "No purchasable offer.");
      return;
    }

    await buyBtn.click();
    await page.waitForURL(/\/marketplace\/buy\/\d+/, { timeout: 10_000 });
    await expect(page.locator(".tibia-pay-shell")).toBeVisible({ timeout: 10_000 });

    // Seller section should be rendered
    const sellerSection = page.locator(".tibia-pay-seller-highlight, .tibia-pay-seller-inline, .tibia-pay-seller-link-wrap");
    await expect(sellerSection.first()).toBeVisible({ timeout: 8_000 });
  });

  test("payment page shows Testnet option when Testnet gateway is active", async ({ page }) => {
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    if (!(await firstCard.count())) {
      test.skip(true, "No servers seeded.");
      return;
    }

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    if (!(await itemCard.count())) {
      test.skip(true, "No items listed.");
      return;
    }

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    const buyBtn = page.locator("button.offer-card-v2-buy-btn").filter({ hasNotText: /cancelar/i }).first();
    if (!(await buyBtn.count())) {
      test.skip(true, "No purchasable offer.");
      return;
    }

    await buyBtn.click();
    await page.waitForURL(/\/marketplace\/buy\/\d+/, { timeout: 10_000 });
    await expect(page.locator(".tibia-pay-shell")).toBeVisible({ timeout: 10_000 });

    // Gateway select should have Testnet option
    const gatewaySelect = page.locator("select#payment-method, select[aria-label*='gateway' i], select.tibia-pay-select").first();
    await expect(gatewaySelect).toBeVisible({ timeout: 8_000 });
    const options = await gatewaySelect.locator("option").allTextContents();
    expect(options.some(o => /testnet/i.test(o))).toBe(true);

    // Default selection should be Pix (not BTCPayServer)
    const selectedValue = await gatewaySelect.inputValue();
    expect(selectedValue).not.toBe("BTCPayServer");
  });

  test("generate Testnet address shows address and private key", async ({ page }) => {
    await page.goto("/servers");
    const firstCard = page.locator(".world-select-card").first();
    if (!(await firstCard.count())) {
      test.skip(true, "No servers seeded.");
      return;
    }

    await firstCard.locator(".world-select-join").first().click();
    await page.waitForURL(/\/servers\/\d+/, { timeout: 10_000 });

    const itemCard = page.locator(".server-auction-card, .offer-card-v2, [class*='auction-card']").first();
    if (!(await itemCard.count())) {
      test.skip(true, "No items listed.");
      return;
    }

    await itemCard.click();
    await page.waitForURL(/\/servers\/\d+\/items\//, { timeout: 10_000 });

    const buyBtn = page.locator("button.offer-card-v2-buy-btn").filter({ hasNotText: /cancelar/i }).first();
    if (!(await buyBtn.count())) {
      test.skip(true, "No purchasable offer.");
      return;
    }

    await buyBtn.click();
    await page.waitForURL(/\/marketplace\/buy\/\d+/, { timeout: 10_000 });
    await expect(page.locator(".tibia-pay-shell")).toBeVisible({ timeout: 10_000 });

    // Select Testnet gateway
    const gatewaySelect = page.locator("select#payment-method, select.tibia-pay-select").first();
    await expect(gatewaySelect).toBeVisible({ timeout: 8_000 });
    const options = await gatewaySelect.locator("option").allTextContents();
    if (!options.some(o => /testnet/i.test(o))) {
      test.skip(true, "Testnet gateway not available in this environment.");
      return;
    }
    await gatewaySelect.selectOption({ label: /testnet/i });

    // Click generate payment button
    const generateBtn = page.locator("button[aria-label*='Gerar' i], button.tibia-pay-btn").first();
    await expect(generateBtn).toBeVisible({ timeout: 8_000 });
    await generateBtn.click();

    // Should show a generated Bitcoin address (Testnet addresses start with tb1q, m or n)
    const addressBlock = page.locator(".tibia-pay-address");
    await expect(addressBlock).toBeVisible({ timeout: 15_000 });
    const address = await addressBlock.textContent();
    expect(address?.trim().length).toBeGreaterThan(10);

    // Testnet payment also shows private key
    const privateKeyBlock = page.locator(".tibia-pay-private-key");
    await expect(privateKeyBlock).toBeVisible({ timeout: 8_000 });
  });
});
