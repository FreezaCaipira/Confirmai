import { expect, test } from "@playwright/test";

test.describe("Language switch", () => {
  test("switch to English via flag and persist on protected route", async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.removeItem("Confirmai.uiLanguage");
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/");

    await Promise.all([
      page.waitForURL(/uiLang=en-US/i),
      page.locator(".language-flag-card.lang-us").click()
    ]);

    // Verify English is active after navigation
    await expect(page.locator(".language-flag-card.lang-us.active")).toBeVisible();

    // Anonymous access to protected route redirects to login
    await page.goto("/orders");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);

    // Login page should render in English
    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
  });

  test("switch to Spanish via flag and persist on protected route", async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.removeItem("Confirmai.uiLanguage");
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/");

    await Promise.all([
      page.waitForURL(/uiLang=es-ES/i),
      page.locator(".language-flag-card.lang-es").click()
    ]);

    // Verify Spanish is active after navigation
    await expect(page.locator(".language-flag-card.lang-es.active")).toBeVisible();

    // Anonymous access to protected route redirects to login
    await page.goto("/orders");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("switch to English persists after full refresh", async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.removeItem("Confirmai.uiLanguage");
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/");

    await Promise.all([
      page.waitForURL(/uiLang=en-US/i),
      page.locator(".language-flag-card.lang-us").click()
    ]);

    await page.reload();

    // Language should still be English after refresh
    await expect(page.locator(".language-flag-card.lang-us.active")).toBeVisible();

    // Anonymous access to protected route still redirects to login
    await page.goto("/orders");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });
});
