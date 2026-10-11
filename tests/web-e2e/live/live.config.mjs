import { defineConfig, devices } from '@playwright/test';

// Searches the real Open Food Facts service, so it needs internet access. BASE_URL picks the site to test:
// the local build by default, or the deployed GitHub Pages site.
const port = 5180;
const baseURL = process.env.BASE_URL ?? `http://localhost:${port}/`;

export default defineConfig({
  testDir: '.',
  timeout: 120_000,
  workers: 1,
  reporter: 'list',
  use: {
    ...devices['Desktop Chrome'],
    baseURL,
    launchOptions: process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {},
    screenshot: 'on',
  },
  webServer: process.env.BASE_URL ? undefined : {
    command: `node ../serve.mjs ${process.env.WEB_ROOT ?? '../../../publish/web/wwwroot'} ${port}`,
    url: `http://localhost:${port}/`,
  },
});
