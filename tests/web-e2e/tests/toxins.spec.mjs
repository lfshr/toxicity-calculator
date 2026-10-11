import { test, expect } from '@playwright/test';
import { open, setPatient, stubOpenFoodFacts } from './helpers.mjs';

test('home lists every calculator', async ({ page }) => {
  await open(page);
  for (const name of ['Chocolate', 'Xylitol', 'Ibuprofen', 'Paracetamol', 'Rat and mouse bait',
    'Emetics and charcoal', 'CPR drugs', 'Drug dose', 'Constant rate infusion', 'Fluid therapy', 'Blood transfusion',
    'Body surface area and calories', 'Unit converter']) {
    await expect(page.locator('.calc-card strong', { hasText: new RegExp(`^${name}$`) })).toBeVisible();
  }
});

test('chocolate: Vets Now worked example needs emesis', async ({ page }) => {
  await open(page, 'toxins/chocolate');
  await page.locator('#weight').fill('10');
  await page.locator('#type').selectOption('Milk');
  await page.locator('#amount').fill('100');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.locator('.verdict-title')).toHaveText('Consider inducing emesis');
  await expect(page.locator('#dose')).toHaveText('23 mg/kg');
  await expect(page.getByText('100 g of milk chocolate eaten by a 10 kg dog.')).toBeVisible();
});

test('chocolate: below threshold is not an emesis case', async ({ page }) => {
  await open(page, 'toxins/chocolate');
  await page.locator('#weight').fill('30');
  await page.locator('#type').selectOption('White');
  await page.locator('#amount').fill('200');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.locator('.verdict-title')).toHaveText('Emesis not indicated by dose');
  await expect(page.locator('.verdict')).toHaveClass(/risk-low/);
});

test('chocolate: missing fields show errors', async ({ page }) => {
  await open(page, 'toxins/chocolate');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.getByText("Enter the dog's weight in kg.")).toBeVisible();
  await expect(page.getByText('Choose the type of chocolate.')).toBeVisible();
  await expect(page.getByText('Enter the amount eaten in grams.')).toBeVisible();
});

test('chocolate: product search fills in cocoa and pack weight', async ({ page }) => {
  const requests = await stubOpenFoodFacts(page);
  await open(page, 'toxins/chocolate');
  await page.locator('#weight').fill('8');
  await page.getByRole('button', { name: 'Search products' }).click();

  await page.getByLabel('Product name').fill('lindt 70');
  await page.getByRole('button', { name: 'Search', exact: true }).click();
  // The biscuits have no cocoa data, so only the two chocolates are listed.
  await expect(page.locator('.result')).toHaveCount(2);
  // Only Open Food Facts' main site allows requests from a browser, so search goes straight there.
  expect(requests.filter(url => url.includes('openfoodfacts.org/cgi/search.pl'))).toHaveLength(1);
  expect(requests.filter(url => url.includes('search.openfoodfacts.org'))).toHaveLength(0);
  await page.locator('.result', { hasText: 'Excellence 70% Cacao' }).click();

  await expect(page.locator('#cocoa')).toHaveValue('70');
  await expect(page.getByText('Excellence 70% Cacao (Lindt): 70% cocoa, taken from the label data.')).toBeVisible();
  await page.getByRole('button', { name: 'Use whole pack (100 g)' }).click();
  await expect(page.locator('#amount')).toHaveValue('100');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.locator('.verdict')).toHaveClass(/risk-critical/);
});

test('chocolate: product search looks the brand up when full-text search is busy', async ({ page }) => {
  const requests = await stubOpenFoodFacts(page);
  // Open Food Facts' busy page has no CORS headers, so the browser reports a failed request.
  await page.route('https://world.openfoodfacts.org/cgi/search.pl**', route => route.abort('failed'));
  await open(page, 'toxins/chocolate');
  await page.getByRole('button', { name: 'Search products' }).click();

  await page.getByLabel('Product name').fill('lindt 70');
  await page.getByRole('button', { name: 'Search', exact: true }).click();

  // Three tries a few seconds apart, then the brand lookup.
  await expect(page.locator('.result')).toHaveCount(2, { timeout: 15_000 });
  expect(requests.filter(url => url.includes('/cgi/search.pl'))).toHaveLength(3);
  expect(requests.filter(url => url.includes('/api/v2/search?brands_tags=lindt-70%7Clindt'))).toHaveLength(1);
});

test('xylitol from a known amount', async ({ page }) => {
  await open(page, 'toxins/xylitol');
  await page.locator('#weight').fill('10');
  await page.getByRole('radio', { name: 'Known amount' }).click();
  await page.locator('#xylitol').fill('1.5');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.locator('#dose')).toHaveText('150 mg/kg');
  await expect(page.locator('.verdict-title')).toHaveText('Consider inducing emesis');
});

test('ibuprofen and paracetamol keep separate forms', async ({ page }) => {
  await open(page, 'toxins/ibuprofen');
  await expect(page.locator('#strength')).toHaveValue('200');
  await page.locator('#weight').fill('10');
  await page.locator('#count').fill('2');
  await page.getByRole('button', { name: 'Calculate' }).click();
  await expect(page.locator('#dose')).toHaveText('40 mg/kg');

  await page.goto('toxins/paracetamol');
  await expect(page.locator('h1')).toHaveText('Paracetamol');
  await expect(page.locator('#strength')).toHaveValue('500');
});

test('rat bait fills in the usual concentration', async ({ page }) => {
  await open(page, 'toxins/rat-bait');
  await page.locator('#weight').fill('10');
  await page.locator('#ingredient').selectOption('Brodifacoum');
  await expect(page.locator('#concentration')).toHaveValue('0.005');
  await page.locator('#bait').fill('50');
  await page.getByRole('button', { name: 'Calculate' }).click();

  await expect(page.locator('#dose')).toHaveText('0.25 mg/kg');
});

test('the patient weight and species carry between calculators', async ({ page }) => {
  await open(page, 'toxins/xylitol');
  await setPatient(page, '12', 'Cat');
  await page.getByRole('link', { name: 'All calculators' }).click();
  await page.locator('.calc-card', { hasText: 'Fluid therapy' }).click();
  await expect(page.locator('#weight')).toHaveValue('12');
  await expect(page.getByRole('radio', { name: 'Cat', exact: true })).toHaveAttribute('aria-checked', 'true');
  await page.locator('#weight').fill('14');
  await expect(page.getByText('30.9 lb. Shared with the other calculators.')).toBeVisible();
  await page.getByRole('link', { name: 'All calculators' }).click();
  await page.locator('.calc-card', { hasText: 'Xylitol' }).click();
  await expect(page.locator('#weight')).toHaveValue('14');
  await expect(page.getByText("Cat's weight")).toBeVisible();
});

test('the header has no patient fields', async ({ page }) => {
  await open(page, 'toxins/xylitol');
  await setPatient(page, '4', 'Cat');
  await expect(page.locator('.app-header input')).toHaveCount(0);
  await expect(page.locator('.app-header [role="radio"]')).toHaveCount(0);
  await expect(page.getByText('New patient')).toHaveCount(0);
});

test('chocolate is dogs only: Cat is greyed out, and the patient stays a cat elsewhere', async ({ page }) => {
  await open(page, 'fluids');
  await setPatient(page, '4.25', 'Cat');
  await page.getByRole('link', { name: 'All calculators' }).click();
  await page.locator('.calc-card', { hasText: 'Chocolate' }).click();
  const cat = page.getByRole('radio', { name: 'Cat', exact: true });
  await expect(cat).toBeDisabled();
  await expect(page.getByRole('radio', { name: 'Dog', exact: true })).toHaveAttribute('aria-checked', 'true');
  await expect(page.getByText('Dogs only: there are no published cat thresholds for chocolate.')).toBeVisible();
  await expect(page.getByText("Dog's weight")).toBeVisible();
  await page.locator('#type').selectOption('Milk');
  await page.locator('#amount').fill('12.5');
  await page.getByRole('button', { name: 'Calculate' }).click();
  await expect(page.getByText('12.5 g of milk chocolate eaten by a 4.25 kg dog.')).toBeVisible();

  await page.getByRole('link', { name: 'Back to calculator' }).first().click();
  await page.getByRole('link', { name: 'All calculators' }).click();
  await page.locator('.calc-card', { hasText: 'Fluid therapy' }).click();
  await expect(page.getByRole('radio', { name: 'Cat', exact: true })).toHaveAttribute('aria-checked', 'true');
});

test('rat bait in a cat only offers ingredients with cat thresholds', async ({ page }) => {
  await open(page, 'toxins/rat-bait');
  await setPatient(page, '4', 'Cat');
  await expect(page.locator('#ingredient option', { hasText: 'Brodifacoum (dogs only)' })).toBeDisabled();
  await expect(page.locator('#ingredient option', { hasText: 'Cholecalciferol' })).toBeDisabled();
  await page.locator('#ingredient').selectOption('Warfarin');
  await expect(page.getByText(/Cat thresholds for warfarin/)).toBeVisible();
  await page.locator('#bait').fill('10');
  await page.getByRole('button', { name: 'Calculate' }).click();

  // 10 g of 0.025% warfarin in a 4 kg cat is 0.625 mg/kg: toxic in a cat (from 0.5), not yet in a dog (from 2).
  await expect(page.locator('#dose')).toHaveText('0.625 mg/kg');
  await expect(page.getByText('Toxic dose', { exact: true })).toBeVisible();
  await expect(page.getByText(/lowest cat LD50 5 mg\/kg/)).toBeVisible();
});

test('rat bait greys out Cat for an ingredient with no cat thresholds', async ({ page }) => {
  await open(page, 'toxins/rat-bait');
  await setPatient(page, '10', 'Dog');
  await page.locator('#ingredient').selectOption('Brodifacoum');
  await expect(page.getByRole('radio', { name: 'Cat', exact: true })).toBeDisabled();
  await expect(page.getByText(/Dogs only for brodifacoum/)).toBeVisible();
  await page.locator('#ingredient').selectOption('Diphacinone');
  await expect(page.getByRole('radio', { name: 'Cat', exact: true })).toBeEnabled();
});

test('paracetamol in a cat uses cat guidance, not the dog thresholds', async ({ page }) => {
  await open(page, 'toxins/paracetamol');
  await setPatient(page, '4', 'Cat');
  await expect(page.getByText('Paracetamol has no safe dose in cats')).toBeVisible();
  await page.locator('#count').fill('0.25');
  await page.getByRole('button', { name: 'Calculate' }).click();

  // A quarter of a 500 mg tablet is 31.3 mg/kg: below the dog threshold, but toxic in a cat.
  await expect(page.locator('#dose')).toHaveText('31.3 mg/kg');
  await expect(page.locator('.verdict')).toContainText('Consider inducing emesis');
  await expect(page.locator('.verdict')).toContainText('no established safe dose of paracetamol in cats');
  await expect(page.getByText('Emesis not indicated')).toHaveCount(0);
});

test('xylitol in a cat says cats are not at risk', async ({ page }) => {
  await open(page, 'toxins/xylitol');
  await setPatient(page, '4', 'Cat');
  await expect(page.getByText('cats are not at risk of xylitol hypoglycaemia or liver')).toBeVisible();
});

test('a result opened directly goes home', async ({ page }) => {
  await open(page, 'result');
  await expect(page).toHaveURL(/\/$/);
});
