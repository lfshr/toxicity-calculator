import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const fixture = name => readFileSync(fileURLToPath(new URL(`../fixtures/${name}`, import.meta.url)), 'utf8');
const icon = readFileSync(fileURLToPath(new URL('../../../src/ChocolateToxicity.Web/wwwroot/icon-192.png', import.meta.url)));

/**
 * Answers Open Food Facts requests from fixtures, so the tests don't depend on the live service
 * (or on network access at all).
 */
export async function stubOpenFoodFacts(page) {
  const requests = [];
  page.on('request', request => requests.push(request.url()));
  // Search-a-licious sends no CORS headers, so a browser blocks it. Playwright doesn't apply CORS to fulfilled
  // responses, so fail the request the way the browser would.
  await page.route('https://search.openfoodfacts.org/**', route => route.abort('failed'));
  await page.route('https://world.openfoodfacts.org/cgi/search.pl**', route =>
    route.fulfill({ contentType: 'application/json', body: fixture('search.json'), headers: { 'Access-Control-Allow-Origin': '*' } }));
  await page.route('https://world.openfoodfacts.org/api/v2/product/**', route =>
    route.fulfill({ contentType: 'application/json', body: fixture('product-lindt.json'), headers: { 'Access-Control-Allow-Origin': '*' } }));
  await page.route('https://images.openfoodfacts.org/**', route =>
    route.fulfill({ contentType: 'image/png', body: icon }));
  return requests;
}

/** Opens a page and waits for Blazor to render it. */
export async function open(page, path = '') {
  await page.goto(path);
  await page.locator('.app-header').waitFor();
}

export async function setPatient(page, weight, species = 'Dog') {
  await page.getByRole('radio', { name: species, exact: true }).click();
  await page.locator('#patient-weight').fill(weight);
}
