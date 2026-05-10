import { expect, test } from "@playwright/test";

test.describe("Cookie consent", () => {
  async function openPreferencesModal(page: import("@playwright/test").Page) {
    const openButton = page.getByRole("button", { name: /Personalizar|Customize/i });
    await expect(async () => {
      await openButton.click();
      await expect(page.locator("#cookie-consent-modal:not(.is-hidden)").first()).toBeVisible({ timeout: 1_500 });
    }).toPass({ timeout: 10_000 });

    return page.locator("#cookie-consent-modal:not(.is-hidden)").first();
  }

  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.addInitScript(() => {
      localStorage.removeItem("Confirmai.cookieConsent.v1");
      localStorage.removeItem("Confirmai.uiLanguage");
    });
    await page.goto("/");
  });

  test("accept all hides banner and persists consent", async ({ page }) => {
    const banner = page.locator("#cookie-consent-banner");
    await expect(banner).toBeVisible();

    const acceptButton = page.getByRole("button", { name: /Aceitar tudo|Accept all|Aceptar todo/i });
    await acceptButton.click();

    await expect(async () => {
      const localConsentRaw = await page.evaluate(() => localStorage.getItem("Confirmai.cookieConsent.v1"));
      expect(localConsentRaw).not.toBeNull();

      const localConsent = JSON.parse(localConsentRaw ?? "{}");
      expect(localConsent.essential).toBe(true);
      expect(localConsent.analytics).toBe(true);

      await expect(banner).toBeHidden({ timeout: 1_500 });
    }).toPass({ timeout: 8_000 });

    const localConsent = await page.evaluate(() => localStorage.getItem("Confirmai.cookieConsent.v1"));
    expect(localConsent).not.toBeNull();

    const cookie = (await page.context().cookies()).find(c => c.name === "Confirmai.cookieConsent");
    expect(cookie?.value).toBe("all");
  });

  test("customize opens modal and save closes modal", async ({ page }) => {
    let modal = await openPreferencesModal(page);
    await expect(modal).toBeVisible();

    // Use JS to click the toggle directly to avoid detach-on-rerender race
    await page.evaluate(() => {
      const toggle = document.getElementById("cookie-analytics-toggle") as HTMLInputElement | null;
      if (toggle) toggle.click();
    });

    await expect(async () => {
      modal = page.locator("#cookie-consent-modal:not(.is-hidden)").first();
      const saveButton = modal.getByRole("button", { name: /Salvar preferencias|Save preferences|Guardar preferencias/i });
      await saveButton.click({ timeout: 1_500 });
      await expect(modal).toBeHidden({ timeout: 1_500 });
    }).toPass({ timeout: 12_000 });

    const localConsentRaw = await page.evaluate(() => localStorage.getItem("Confirmai.cookieConsent.v1"));
    expect(localConsentRaw).not.toBeNull();

    const localConsent = JSON.parse(localConsentRaw ?? "{}");
    expect(localConsent.essential).toBe(true);
    expect(localConsent.analytics).toBe(true);

    // Cookie persistence is asserted in the "accept all" scenario; here we validate preference content.
  });

  test("reject optional hides banner and saves analytics disabled", async ({ page }) => {
    const banner = page.locator("#cookie-consent-banner");
    await expect(banner).toBeVisible();

    const rejectButton = page.getByRole("button", { name: /Recusar opcionais|Reject optional|Rechazar opcionales/i });
    await rejectButton.click();

    await expect(banner).toBeHidden();

    const localConsentRaw = await page.evaluate(() => localStorage.getItem("Confirmai.cookieConsent.v1"));
    expect(localConsentRaw).not.toBeNull();

    const localConsent = JSON.parse(localConsentRaw ?? "{}");
    expect(localConsent.essential).toBe(true);
    expect(localConsent.analytics).toBe(false);
  });

  test("cancel on preferences modal closes without persisting consent", async ({ page }) => {
    const modal = await openPreferencesModal(page);
    await expect(modal).toBeVisible();

    const analyticsToggle = modal.locator("#cookie-analytics-toggle");
    await expect(analyticsToggle).toBeVisible();
    await analyticsToggle.click();

    const cancelButton = modal.getByRole("button", { name: /Cancelar|Cancel/i });
    await cancelButton.click();

    await expect(modal).toBeHidden();
    await expect(page.locator("#cookie-consent-banner")).toBeVisible();

    const localConsentRaw = await page.evaluate(() => localStorage.getItem("Confirmai.cookieConsent.v1"));
    expect(localConsentRaw).toBeNull();
  });
});
