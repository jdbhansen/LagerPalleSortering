import { expect, test } from "@playwright/test";

test.describe("UI sanity", () => {
  test("registration and confirmation controls are visible", async ({ page }) => {
    await page.goto("/app");

    await expect(page.getByLabel("Varenummer")).toBeVisible();
    await expect(page.getByLabel("Holdbarhed (YYYYMMDD)")).toBeVisible();
    await expect(page.getByLabel("Antal kolli")).toBeVisible();
    await expect(page.getByRole("button", { name: "Registrer kolli" })).toBeVisible();

    await expect(page.getByLabel("Scannet pallekode")).toBeVisible();
    await expect(page.getByLabel("Antal at bekræfte")).toBeVisible();
    await expect(page.getByRole("button", { name: "Bekræft flyt" })).toBeVisible();
  });

  test("operator can register and confirm move end-to-end", async ({ page }) => {
    await page.goto("/app");

    await page.getByLabel("Varenummer").fill("SANITY-ITEM");
    await page.getByLabel("Holdbarhed (YYYYMMDD)").fill("20261231");
    await page.getByLabel("Antal kolli").fill("2");
    await page.getByRole("button", { name: "Registrer kolli" }).click();

    await expect(page.locator(".alert", { hasText: "læg kolli på" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Print dato-label" })).toBeEnabled();

    await page.getByLabel("Antal at bekræfte").fill("1");
    await page.getByRole("button", { name: "Bekræft flyt" }).click();

    await expect(page.locator(".alert", { hasText: "Flytning bekræftet" })).toBeVisible();
  });

  test("advanced section shows date barcode column in recent entries", async ({ page }) => {
    await page.goto("/app");

    await page.getByLabel("Varenummer").fill("SANITY-DATE");
    await page.getByLabel("Holdbarhed (YYYYMMDD)").fill("20270115");
    await page.getByLabel("Antal kolli").fill("1");
    await page.getByRole("button", { name: "Registrer kolli" }).click();

    await expect(page.getByRole("columnheader", { name: "Datostregkode" })).toBeVisible();
    await expect(page.locator("table.table tbody tr td svg").first()).toBeVisible();
  });

  test("simple mode hides advanced panels and keeps scanner flow operational", async ({ page }) => {
    await page.goto("/app");

    await page.getByRole("button", { name: "Skift til simpel scanner-visning" }).click();
    await expect(page.getByRole("button", { name: "Skift til avanceret visning" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Fortryd seneste" })).toHaveCount(0);

    await page.getByLabel("Varenummer").fill("SIMPLE-ITEM");
    await page.getByLabel("Holdbarhed (YYYYMMDD)").fill("20270201");
    await page.getByLabel("Antal kolli").fill("1");
    await page.getByRole("button", { name: "Registrer kolli" }).click();
    const suggestionAlert = page.locator(".alert", { hasText: "læg kolli på" });
    await expect(suggestionAlert).toBeVisible();
    const suggestionText = (await suggestionAlert.textContent()) ?? "";
    const palletMatch = suggestionText.match(/P-\d{3}/);
    expect(palletMatch).not.toBeNull();

    await page.getByLabel("Scannet pallekode").fill(`PALLET:${palletMatch![0]}`);
    await page.getByLabel("Antal at bekræfte").fill("1");
    await page.getByRole("button", { name: "Bekræft flyt" }).click();
    await expect(page.locator(".alert", { hasText: "Flytning bekræftet" })).toBeVisible();
  });
});
