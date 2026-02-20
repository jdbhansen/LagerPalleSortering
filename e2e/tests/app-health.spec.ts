import { expect, test } from "@playwright/test";

test("home page loads new pallet sorting view", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveURL(/\/app$/);

  await expect(page.getByRole("heading", { name: "Ny pallesortering" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Start ny pallesortering" })).toBeVisible();
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
