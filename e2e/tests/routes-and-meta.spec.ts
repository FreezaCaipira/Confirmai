import { expect, test } from "@playwright/test";

test.describe("Redirecting routes", () => {
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

  test("/marketplace redirects to /servers", async ({ page }) => {
    await page.goto("/marketplace");
    await expect(page).toHaveURL(/\/servers/i);
  });

  test("/marketplace?serverId=1 redirects through /servers/1", async ({ page }) => {
    await page.goto("/marketplace?serverId=1");
    // /servers/{id} requires [Authorize], so anonymous users end up at login
    await expect(page).toHaveURL(/\/servers\/1|Identity\/Account\/Login/i);
  });

  test("/products redirects to /servers", async ({ page }) => {
    await page.goto("/products");
    await expect(page).toHaveURL(/\/servers/i);
  });
});

test.describe("Extended auth guards", () => {
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

  test("anonymous /servers/1 loads as a public page", async ({ page }) => {
    await page.goto("/servers/1");
    await expect(page).not.toHaveURL(/\/Identity\/Account\/Login/i);
    await expect(page.locator("header.oldsite-header")).toBeVisible();
  });

  test("anonymous /admin/products redirects to login", async ({ page }) => {
    await page.goto("/admin/products");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/logs redirects to login", async ({ page }) => {
    await page.goto("/admin/logs");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/gateways redirects to login", async ({ page }) => {
    await page.goto("/admin/gateways");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/delivery-agents redirects to login", async ({ page }) => {
    await page.goto("/admin/delivery-agents");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/server-requests redirects to login", async ({ page }) => {
    await page.goto("/admin/server-requests");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/languages redirects to login", async ({ page }) => {
    await page.goto("/admin/languages");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("anonymous /admin/orders-review redirects to login", async ({ page }) => {
    await page.goto("/admin/orders-review");
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });
});

test.describe("Viewport and meta tags", () => {
  test("viewport meta tag is present", async ({ page }) => {
    await page.goto("/servers");

    const viewport = page.locator('meta[name="viewport"]');
    await expect(viewport).toHaveAttribute("content", /width=device-width/);
  });

  test("charset meta tag is present", async ({ page }) => {
    await page.goto("/servers");

    const charset = page.locator('meta[charset="utf-8"]');
    await expect(charset).toHaveCount(1);
  });

  test("page has lang attribute", async ({ page }) => {
    await page.goto("/servers");

    const html = page.locator("html");
    await expect(html).toHaveAttribute("lang", /\w+/);
  });
});
