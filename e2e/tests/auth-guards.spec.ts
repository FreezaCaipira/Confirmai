import { expect, test } from "@playwright/test";

test.describe("Authentication and guards", () => {
  test("login page renders core fields", async ({ page }) => {
    await page.goto("/Identity/Account/Login");

    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
    await expect(page.locator("input[name='Input.Password']")).toBeVisible();
    await expect(page.locator("#Input_RememberMe")).toBeVisible();

    const submitButton = page.locator("form button[type='submit']");
    await expect(submitButton).toBeVisible();

    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
  });

  test("register page renders core fields", async ({ page }) => {
    await page.goto("/Identity/Account/Register");

    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
    await expect(page.locator("input[name='Input.Password']")).toBeVisible();
    await expect(page.locator("input[name='Input.ConfirmPassword']")).toBeVisible();

    const submitButton = page.locator("form button[type='submit']");
    await expect(submitButton).toBeVisible();

    await expect(page).toHaveURL(/\/Identity\/Account\/Register/i);
  });

  test("anonymous user is redirected to login from admin route", async ({ page }) => {
    await page.context().clearCookies();
    await page.goto("/admin/users");

    // Unauthenticated users are redirected to the login page
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
  });
});
