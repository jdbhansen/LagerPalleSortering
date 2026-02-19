import { expect, test } from "@playwright/test";

test.describe("UI sanity", () => {
  test("operator can run the new pallet sorting flow end-to-end", async ({ page }) => {
    await page.goto("/app");

    await page.getByRole("button", { name: "Start ny pallesortering" }).click();

    await page.getByLabel("Kolli stregkode").fill("SANITY-ITEM");
    await page.getByLabel("Holdbarhed (YYYYMMDD)").fill("20261231");
    await page.getByRole("button", { name: "Registrer kolli" }).click();

    const registerAlert = page.locator(".alert", { hasText: "læg kolli på" });
    await expect(registerAlert).toBeVisible();
    const registerAlertText = await registerAlert.textContent();
    const palletMatch = (registerAlertText ?? "").match(/P-\d{3}/);
    expect(palletMatch).not.toBeNull();

    await page.getByLabel("Palle stregkode").fill(`PALLET:${palletMatch![0]}`);
    await page.getByRole("button", { name: "Sæt kolli på plads" }).click();

    await expect(page.locator(".alert", { hasText: "Flytning bekræftet" })).toBeVisible();
  });
});
