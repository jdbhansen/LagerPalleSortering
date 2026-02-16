import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e/tests",
  fullyParallel: false,
  workers: 1,
  retries: 1,
  use: {
    baseURL: "http://127.0.0.1:5099",
    trace: "retain-on-failure"
  },
  webServer: {
    command: "dotnet run --project ./LagerPalleSortering.csproj --no-launch-profile --urls http://127.0.0.1:5099",
    timeout: 120000,
    reuseExistingServer: false
  }
});
