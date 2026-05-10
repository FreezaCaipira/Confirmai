import { expect, test } from "@playwright/test";

test.describe("Public pages", () => {
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

  test("root path shows servers listing", async ({ page }) => {
    await page.goto("/");
    // Both / and /servers render the same servers listing component
    await expect(page.locator("header.oldsite-header")).toBeVisible();
  });

  test("/servers renders page structure", async ({ page }) => {
    await page.goto("/servers");

    // Header present
    await expect(page.locator("header.oldsite-header")).toBeVisible();

    // Page title visible
    await expect(page.locator("h3.servers-list-title")).toBeVisible();

    // Server grid or empty state should be present
    const grid = page.locator(".world-select-grid");
    const emptyState = page.locator(".market-empty");
    await expect(grid.or(emptyState)).toBeVisible();

    // Footer present
    await expect(page.locator("footer.oldsite-footer")).toBeVisible();
  });

  test("/about redirects to /servers", async ({ page }) => {
    await page.goto("/about");
    await expect(page).toHaveURL(/\/servers/i);
  });

  test("/contact redirects to /servers", async ({ page }) => {
    await page.goto("/contact");
    await expect(page).toHaveURL(/\/servers/i);
  });

  test("anonymous /orders redirects to login", async ({ page }) => {
    await page.goto("/orders");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
  });

  test("anonymous /payments redirects to login", async ({ page }) => {
    await page.goto("/payments");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /mailbox redirects to login", async ({ page }) => {
    await page.goto("/mailbox");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin redirects to login", async ({ page }) => {
    await page.goto("/admin");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/orders redirects to login", async ({ page }) => {
    await page.goto("/admin/orders");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /servers/admin-request redirects to login", async ({ page }) => {
    await page.goto("/servers/admin-request");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });
});
