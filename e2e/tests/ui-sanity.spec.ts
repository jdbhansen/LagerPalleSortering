import { expect, test } from "@playwright/test";

test.describe("UI sanity", () => {
  test("registration and confirmation controls are visible", async ({ page }) => {
    await page.goto("/");

    await expect(page.locator("#productInput")).toBeVisible();
    await expect(page.locator("#expiryInput")).toBeVisible();
    await expect(page.locator("#quantityInput")).toBeVisible();
    await expect(page.locator("#registerButton")).toBeVisible();

    await expect(page.locator("#palletScanInput")).toBeVisible();
    await expect(page.locator("#confirmCountInput")).toBeVisible();
    await expect(page.locator("#confirmMoveButton")).toBeVisible();
  });

  test("operator can type into form fields", async ({ page }) => {
    await page.goto("/");

    await page.fill("#productInput", "SANITY-ITEM");
    await page.fill("#expiryInput", "20261231");
    await page.fill("#quantityInput", "3");
    await page.fill("#palletScanInput", "PALLET:P-001");
    await page.fill("#confirmCountInput", "2");

    await expect(page.locator("#productInput")).toHaveValue("SANITY-ITEM");
    await expect(page.locator("#expiryInput")).toHaveValue("20261231");
    await expect(page.locator("#quantityInput")).toHaveValue("3");
    await expect(page.locator("#palletScanInput")).toHaveValue("PALLET:P-001");
    await expect(page.locator("#confirmCountInput")).toHaveValue("2");
  });

  test("key sections and actions are present", async ({ page }) => {
    await page.goto("/");

    await expect(page.locator(".card-header .fw-semibold", { hasText: "Åbne paller" })).toBeVisible();
    await expect(page.locator(".card-header .fw-semibold", { hasText: "Seneste registreringer" })).toBeVisible();

    await expect(page.locator("#exportCsvButton")).toBeVisible();
    await expect(page.locator("#exportExcelButton")).toBeVisible();
    await expect(page.locator("#backupDbButton")).toBeVisible();
    await expect(page.locator("#clearDatabaseButton")).toBeVisible();
  });
});
