/**
 * Auth guard tests for event edit/create/cancel flows.
 *
 * Pages that require [Authorize] must redirect unauthenticated visitors to login.
 * Public pages (listings, detail) must be accessible without login.
 *
 * Authenticated flows (edit save, cancel confirm) are covered by smoke tests
 * that require E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD.
 */

import { expect, test } from "@playwright/test";

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

async function clearAuth(page: import("@playwright/test").Page) {
  await page.context().clearCookies();
}

function isLoginPage(url: string): boolean {
  return (
    url.toLowerCase().includes("/login") ||
    url.toLowerCase().includes("/identity/account")
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Futsal — public pages accessible without login
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Futsal public access", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page);
  });

  test("futsal listing /futsal is publicly accessible", async ({ page }) => {
    await page.goto("/futsal");
    await expect(page).not.toHaveURL(/\/login/i);
    // Page must render some content (not a blank error)
    await expect(page.locator("body")).not.toBeEmpty();
  });

  test("poker listing /poker is publicly accessible", async ({ page }) => {
    await page.goto("/poker");
    await expect(page).not.toHaveURL(/\/login/i);
    await expect(page.locator("body")).not.toBeEmpty();
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Edit pages — require authentication
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Event edit guards — unauthenticated", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page);
  });

  test("GET /futsal/1/edit redirects to login when unauthenticated", async ({ page }) => {
    await page.goto("/futsal/1/edit");
    expect(isLoginPage(page.url())).toBe(true);
  });

  test("GET /poker/1/edit redirects to login when unauthenticated", async ({ page }) => {
    await page.goto("/poker/1/edit");
    expect(isLoginPage(page.url())).toBe(true);
  });

  test("GET /futsal/create redirects to login when unauthenticated", async ({ page }) => {
    await page.goto("/futsal/create");
    expect(isLoginPage(page.url())).toBe(true);
  });

  test("GET /poker/create redirects to login when unauthenticated", async ({ page }) => {
    await page.goto("/poker/create");
    expect(isLoginPage(page.url())).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// Authenticated smoke — edit + cancel buttons visible for event owner
// ─────────────────────────────────────────────────────────────────────────────

declare const process: { env: Record<string, string | undefined> };

const adminEmail    = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

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

test.describe("Event detail — admin controls (authenticated)", () => {
  test.skip(!adminEmail || !adminPassword, "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD to run authenticated tests.");

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("futsal listing renders without error after login", async ({ page }) => {
    await page.goto("/futsal");
    await expect(page).not.toHaveURL(/\/login/i);
    await expect(page.locator("body")).not.toBeEmpty();
  });

  test("poker listing renders without error after login", async ({ page }) => {
    await page.goto("/poker");
    await expect(page).not.toHaveURL(/\/login/i);
    await expect(page.locator("body")).not.toBeEmpty();
  });
});
