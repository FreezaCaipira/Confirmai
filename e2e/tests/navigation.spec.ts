import { expect, test } from "@playwright/test";

test.describe("Navigation and layout", () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
  });

  test("header shows logo and tagline", async ({ page }) => {
    await page.goto("/servers");

    await expect(page.locator("h1.oldsite-logo")).toContainText("OtServ Market");
    await expect(page.locator(".oldsite-tagline")).toContainText("Classic Trade Portal");
  });

  test("anonymous header shows login link", async ({ page }) => {
    await page.goto("/servers");

    const loginLink = page.locator("a[href='/Identity/Account/Login']");
    await expect(loginLink).toBeVisible();
  });

  test("footer renders with copyright", async ({ page }) => {
    await page.goto("/servers");

    const footer = page.locator("footer.oldsite-footer");
    await expect(footer).toBeVisible();
    await expect(footer).toContainText("Freeza");
  });

  test("language flags are visible", async ({ page }) => {
    await page.goto("/servers");

    const flags = page.locator(".nav-language-flags");
    await expect(flags).toBeVisible();

    await expect(page.locator(".language-flag-card.lang-br")).toBeVisible();
    await expect(page.locator(".language-flag-card.lang-us")).toBeVisible();
    await expect(page.locator(".language-flag-card.lang-es")).toBeVisible();
  });

  test("news ticker is present on homepage", async ({ page }) => {
    await page.goto("/servers");

    await expect(page.locator(".oldsite-news-ticker")).toBeVisible();
  });

  test("clicking logo navigates to servers", async ({ page }) => {
    await page.goto("/servers");

    const logo = page.locator("a.oldsite-logo-wrap");
    await expect(logo).toBeVisible();
    await expect(logo).toHaveAttribute("href", "/servers");
  });
});
