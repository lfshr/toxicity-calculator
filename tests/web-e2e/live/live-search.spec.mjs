import { test, expect } from '@playwright/test';

// Open Food Facts allows about 10 searches a minute, and a busy search can take several requests.
test.afterEach(async () => new Promise(resolve => setTimeout(resolve, 30_000)));

// Searches Open Food Facts for real, logging every request to it so a failure shows what the browser saw.
// The last case blocks full-text search, as when Open Food Facts is busy, so the brand lookup is used.
for (const [query, fullTextDown] of [['Lindt 70', false], ['Dairy Milk', false], ['Dairy Milk', true]]) {
  test(`live search: ${query}${fullTextDown ? ' (brand lookup)' : ''}`, async ({ page }, testInfo) => {
    if (fullTextDown) await page.route('https://world.openfoodfacts.org/cgi/search.pl**', route => route.abort('failed'));
    const log = [];
    page.on('console', message => log.push(`console ${message.type()}: ${message.text()}`));
    page.on('requestfailed', request => log.push(`FAILED ${request.method()} ${request.url()}: ${request.failure()?.errorText}`));
    page.on('response', async response => {
      if (!response.url().includes('openfoodfacts.org') || response.url().includes('images.')) return;
      const headers = response.headers();
      log.push(`${response.status()} ${response.request().method()} ${response.url()} ` +
        `acao=${headers['access-control-allow-origin'] ?? '(none)'} type=${headers['content-type'] ?? ''}`);
    });

    try {
      await page.goto('toxins/chocolate');
      await page.locator('#weight').fill('8');
      await page.getByRole('button', { name: 'Search products' }).click();
      await page.getByLabel('Product name').fill(query);
      await page.getByRole('button', { name: 'Search', exact: true }).click();
      await expect(page.locator('.result').first()).toBeVisible({ timeout: 60_000 });
      log.push(`results: ${await page.locator('.result').count()}`);
      for (const text of (await page.locator('.result').allInnerTexts()).slice(0, 5)) log.push(`  ${text.replace(/\s+/g, ' ')}`);
    } finally {
      log.push(`page message: ${await page.locator('[role=status]').allInnerTexts()}`);
      await page.screenshot({ path: testInfo.outputPath('page.png'), fullPage: true });
      console.log(`--- ${query} ---\n${log.join('\n')}`);
    }
  });
}
