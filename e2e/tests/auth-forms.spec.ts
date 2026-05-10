import { expect, test } from "@playwright/test";

test.describe("Registration form", () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/Identity/Account/Register");
  });

  test("renders all form fields", async ({ page }) => {
    await expect(page.locator("input[name='Input.Email']")).toBeVisible();
    await expect(page.locator("input[name='Input.Password']")).toBeVisible();
    await expect(page.locator("input[name='Input.ConfirmPassword']")).toBeVisible();
    await expect(page.locator("input[name='Input.PixKey']")).toBeVisible();
    await expect(page.locator("button[type='submit']")).toBeVisible();
  });

  test("submitting empty form shows validation errors", async ({ page }) => {
    await page.locator("button[type='submit']").click();

    // At least one validation error should appear
    const errors = page.locator(".text-danger:visible");
    await expect(errors.first()).toBeVisible();
  });

  test("mismatched passwords show validation error", async ({ page }) => {
    await page.locator("input[name='Input.Email']").fill("test@example.com");
    await page.locator("input[name='Input.Password']").fill("Test123!@#");
    await page.locator("input[name='Input.ConfirmPassword']").fill("Different456!@#");

    await page.locator("button[type='submit']").click();

    const errors = page.locator(".text-danger:visible");
    await expect(errors.first()).toBeVisible();
  });

  test("invalid email shows validation error", async ({ page }) => {
    await page.locator("input[name='Input.Email']").fill("not-an-email");
    await page.locator("input[name='Input.Password']").fill("Test123!@#");
    await page.locator("input[name='Input.ConfirmPassword']").fill("Test123!@#");

    await page.locator("button[type='submit']").click();

    // Browser HTML5 email validation or ASP.NET server validation
    const emailInput = page.locator("input[name='Input.Email']");
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    const serverErrors = page.locator(".text-danger, .field-validation-error").filter({ hasText: /.+/ });
    const hasServerError = await serverErrors.count() > 0;
    expect(isInvalid || hasServerError).toBe(true);
  });

  test("link to login page exists", async ({ page }) => {
    const loginLink = page.locator("a[href*='Login']");
    await expect(loginLink).toBeVisible();
  });
});

test.describe("Login form", () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.setItem("Confirmai.cookieConsent.v1", JSON.stringify({
        essential: true,
        analytics: false,
        updatedAtUtc: new Date().toISOString()
      }));
    });
    await page.goto("/Identity/Account/Login");
  });

  test("submitting empty form shows validation errors", async ({ page }) => {
    await page.locator("button[type='submit']").click();

    const errors = page.locator(".text-danger:visible");
    await expect(errors.first()).toBeVisible();
  });

  test("invalid credentials show error message", async ({ page }) => {
    await page.locator("input[name='Input.Email']").fill("nonexistent@example.com");
    await page.locator("input[name='Input.Password']").fill("WrongPassword123!");

    await page.locator("button[type='submit']").click();

    // Should stay on login page with an error
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
    const errors = page.locator(".text-danger:visible, .validation-summary-errors:visible");
    await expect(errors.first()).toBeVisible();
  });

  test("link to register page exists", async ({ page }) => {
    const registerLink = page.locator("a[href*='Register']");
    await expect(registerLink).toBeVisible();
  });

  test("link to forgot password exists", async ({ page }) => {
    const forgotLink = page.locator("a[href*='ForgotPassword']");
    await expect(forgotLink).toBeVisible();
  });
});
