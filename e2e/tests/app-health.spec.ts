import { expect, test } from "@playwright/test";

test("home page loads key operator sections", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveURL(/\/app$/);

  await expect(page.getByRole("heading", { name: "Palle sortering" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Registrer kolli" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Bekræft flytning" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Skift til simpel scanner-visning" })).toBeVisible();
});

test("health and metrics endpoints are reachable", async ({ request }) => {
  const health = await request.get("/health");
  expect(health.ok()).toBeTruthy();
  const healthJson = await health.json();
  expect(healthJson.status).toBeDefined();
  expect(healthJson.warehouse).toBeDefined();
  expect(healthJson.metrics).toBeDefined();

  const metrics = await request.get("/metrics");
  expect(metrics.ok()).toBeTruthy();
  const metricsJson = await metrics.json();
  expect(metricsJson.registerAttempts).toBeDefined();
});
