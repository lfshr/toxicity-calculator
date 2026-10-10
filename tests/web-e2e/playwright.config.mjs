import { defineConfig, devices } from '@playwright/test';

const port = 5180;

export default defineConfig({
  testDir: './tests',
  timeout: 60_000,
  use: {
    baseURL: `http://localhost:${port}/`,
    // Use a preinstalled Chromium when one is given, rather than downloading Playwright's.
    launchOptions: process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {},
    serviceWorkers: 'block',
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'phone', use: { ...devices['Pixel 7'] } },
  ],
  webServer: {
    command: `node serve.mjs ${process.env.WEB_ROOT ?? '../../publish/web/wwwroot'} ${port}`,
    url: `http://localhost:${port}/`,
    reuseExistingServer: !process.env.CI,
  },
});
