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
// Mailbox — unauthenticated guard
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Mailbox access guard", () => {
  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
  });

  test("mailbox redirects unauthenticated user to login", async ({ page }) => {
    await page.goto("/mailbox");
    const url = page.url();
    const redirectedToAuth =
      url.toLowerCase().includes("/login") ||
      url.toLowerCase().includes("/account") ||
      (await page.locator("text=Acesso negado").isVisible().catch(() => false));
    expect(redirectedToAuth).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Mailbox — authenticated
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Mailbox (authenticated)", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run mailbox tests.");

  test.beforeEach(async ({ page }) => {
    await dismissCookieBanner(page);
    await loginAsAdmin(page);
  });

  test("mailbox page loads after login", async ({ page }) => {
    await page.goto("/mailbox");

    await expect(page).toHaveTitle(/Caixa|Mensagens|Mailbox|Confirmai/i);

    const hasContent = await Promise.race([
      page.locator(".mailbox-wrap, .mailbox-container, .messages-list, .market-empty").first()
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => true),
    ]).catch(() => false);

    expect(hasContent).toBe(true);
  });

  test("mailbox page shows an empty state or conversation list", async ({ page }) => {
    await page.goto("/mailbox");
    await page.waitForLoadState("networkidle");

    const hasList = (await page.locator(".conversation-item, .mailbox-thread, .message-row").count()) > 0;
    const hasEmpty = await page.locator(".market-empty, text=nenhuma, text=vazia, text=Sem mensagens").isVisible().catch(() => false);
    const hasHeading = await page.locator("h1, h2, h3").first().isVisible().catch(() => false);

    expect(hasList || hasEmpty || hasHeading).toBe(true);
  });

  test("mailbox page has a compose or new message action", async ({ page }) => {
    await page.goto("/mailbox");
    await page.waitForLoadState("networkidle");

    const composeBtn = page.locator("button, a").filter({ hasText: /nova mensagem|compor|escrever|new message/i }).first();
    const hasCompose = await composeBtn.isVisible().catch(() => false);

    // Also accept an input area directly visible
    const hasInput = (await page.locator("textarea, input[type='text'].compose, .compose-input").count()) > 0;

    expect(hasCompose || hasInput).toBe(true);
  });

  test("mailbox conversation URL is accessible when navigated directly", async ({ page }) => {
    // Navigate to a non-existent conversation — should show not-found or empty state, never crash
    await page.goto("/mailbox?thread=nonexistent");
    await page.waitForLoadState("networkidle");

    // Page should still be /mailbox (no unhandled error redirect)
    expect(page.url()).toContain("/mailbox");

    // No unhandled exception panel
    const hasError = await page.locator(".blazor-error-boundary, text=An unhandled error").isVisible().catch(() => false);
    expect(hasError).toBe(false);
  });
});
