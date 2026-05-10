import { expect, test } from "@playwright/test";

test.describe("Forgot password flow", () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/Identity/Account/ForgotPassword");
  });

  test("renders forgot password form", async ({ page }) => {
    await expect(page.locator("h2")).toBeVisible();
    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
    await expect(page.locator("button[type='submit']")).toBeVisible();
  });

  test("submitting empty email shows validation error", async ({ page }) => {
    await page.locator("button[type='submit']").click();

    // Either HTML5 required validation or server-side .text-danger errors
    const emailInput = page.locator("input[name='Input.Email']");
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    const serverErrors = page.locator(".text-danger, .field-validation-error").filter({ hasText: /.+/ });
    const hasServerError = await serverErrors.count() > 0;
    expect(isInvalid || hasServerError).toBe(true);
  });

  test("submitting invalid email shows validation error", async ({ page }) => {
    await page.locator("input[name='Input.Email']").fill("not-an-email");
    await page.locator("button[type='submit']").click();

    // Browser HTML5 email validation or jQuery/server-side validation
    const emailInput = page.locator("input[name='Input.Email']");
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    const serverErrors = page.locator(".field-validation-error, .text-danger").filter({ hasText: /.+/ });
    const hasServerError = await serverErrors.count() > 0;
    expect(isInvalid || hasServerError).toBe(true);
  });

  test("submitting valid email redirects to confirmation", async ({ page }) => {
    // Even with a non-existing email, the app should redirect to confirmation
    // (to prevent email enumeration)
    await page.locator("input[name='Input.Email']").fill("nonexist@example.com");
    await page.locator("button[type='submit']").click();

    await expect(page).toHaveURL(/ForgotPasswordConfirmation/i);
  });

  test("has link back to login", async ({ page }) => {
    const loginLink = page.locator("a[href*='Login']");
    await expect(loginLink).toBeVisible();
  });

  test("has link to register page", async ({ page }) => {
    const registerLink = page.locator("a[href*='Register']");
    await expect(registerLink).toBeVisible();
  });
});

test.describe("Forgot password confirmation page", () => {
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

  test("confirmation page renders after submission", async ({ page }) => {
    await page.goto("/Identity/Account/ForgotPassword");
    await page.locator("input[name='Input.Email']").fill("test@example.com");
    await page.locator("button[type='submit']").click();

    await expect(page).toHaveURL(/ForgotPasswordConfirmation/i);
    // Should show some confirmation text
    const body = page.locator("body");
    await expect(body).toContainText(/.+/);
  });
});
