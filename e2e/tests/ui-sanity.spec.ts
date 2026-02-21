import { expect, test } from "@playwright/test";

test.describe("UI sanity", () => {
  test.beforeEach(async ({ request }) => {
    // Isolate test state so scanner-flow assertions are deterministic across retries.
    const clearResponse = await request.post("/api/v1/warehouse/clear");
    expect(clearResponse.ok()).toBeTruthy();
  });

  test("operator can run the new pallet sorting flow end-to-end", async ({ page, request }) => {
    // Seed one existing open pallet to avoid auto-navigation to print label when first pallet is created.
    const seedRegisterResponse = await request.post("/api/v1/warehouse/register", {
      data: { productNumber: "SANITY-ITEM", expiryDateRaw: "20261231", quantity: 1 },
    });
    expect(seedRegisterResponse.ok()).toBeTruthy();
    const seedRegisterPayload = await seedRegisterResponse.json() as { palletId?: string };
    expect(seedRegisterPayload.palletId).toBeTruthy();

    await page.goto("/app");

    await page.getByRole("button", { name: "Start ny pallesortering" }).click();

    await page.getByLabel("Kolli stregkode").fill("SANITY-ITEM");
    await page.getByLabel("Holdbarhed (YYYYMMDD)").fill("20261231");
    await page.getByRole("button", { name: "Registrer kolli" }).click();

    const palletCodeInput = page.getByLabel("Palle stregkode");
    await expect(palletCodeInput).toHaveAttribute("placeholder", /PALLET:P-\d{3}/);
    const placeholder = await palletCodeInput.getAttribute("placeholder");
    const palletMatch = (placeholder ?? "").match(/P-\d{3}/);
    expect(palletMatch).not.toBeNull();

    await palletCodeInput.fill(`PALLET:${palletMatch![0]}`);
    await page.getByRole("button", { name: "Sæt kolli på plads" }).click();

    await expect(page.locator(".alert", { hasText: "Flytning bekræftet" })).toBeVisible();
  });
});
