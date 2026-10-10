import { test, expect } from '@playwright/test';
import { open, setPatient } from './helpers.mjs';

test('emetics for a 20 kg dog', async ({ page }) => {
  await open(page, 'emesis');
  await setPatient(page, '20');

  await expect(page.locator('#apomorphine-sc')).toHaveText('2 mg = 2 mL');
  await expect(page.locator('#clevor')).toHaveText('3 drops');
  await expect(page.locator('#charcoal')).toHaveText('20-40 g');
});

test('Clevor has no dose under 1.8 kg', async ({ page }) => {
  await open(page, 'emesis');
  await setPatient(page, '1.5');
  await expect(page.getByText("No dose: Clevor's data sheet covers dogs from 1.8 to 100 kg.")).toBeVisible();
});

test('CPR drugs for a 10 kg dog, with an editable concentration', async ({ page }) => {
  await open(page, 'emergency');
  await setPatient(page, '10');

  const row = name => page.locator('#cpr-table tr').filter({ has: page.locator('td strong', { hasText: new RegExp(`^${name}`) }) });
  const adrenaline = row('Adrenaline');
  await expect(adrenaline.locator('.volume')).toHaveText('0.1 mL');
  const atropine = row('Atropine');
  await expect(atropine.locator('.volume')).toHaveText('0.926 mL');
  await atropine.getByLabel('Atropine concentration').fill('0.6');
  await expect(atropine.locator('.volume')).toHaveText('0.833 mL');
  await expect(page.locator('#defib')).toHaveText('External: 20-40 J');
  await page.getByRole('radio', { name: 'Monophasic' }).click();
  await expect(page.locator('#defib')).toHaveText('External: 40-60 J');
});

test('drug dose to mL and tablets', async ({ page }) => {
  await open(page, 'dose');
  await setPatient(page, '20');
  await page.locator('#dose').fill('2');
  await page.locator('#concentration').fill('10');
  await page.locator('#tablet').fill('25');

  const result = page.locator('#dose-result');
  await expect(result).toContainText('40 mg');
  await expect(result).toContainText('4 mL');
  await expect(result).toContainText('1.5 tablets');
});

test('lidocaine CRI added to a bag', async ({ page }) => {
  await open(page, 'cri');
  await setPatient(page, '20');
  await page.locator('#dose').fill('50');
  await page.locator('#concentration').fill('20');
  await page.getByRole('radio', { name: 'Added to fluids' }).click();
  await page.locator('#volume').fill('500');
  await page.locator('#rate').fill('50');

  await expect(page.locator('#cri-result')).toContainText('Add 30 mL (600 mg)');
});

test('fluid plan for a dehydrated dog', async ({ page }) => {
  await open(page, 'fluids');
  await setPatient(page, '20');
  await page.locator('#dehydration').fill('6');
  await page.locator('#hours').fill('12');

  // Maintenance 132 x 20^0.75 / 24 = 52 mL/hr, plus 1200 mL over 12 hours = 100 mL/hr.
  await expect(page.locator('#fluid-result .big')).toHaveText('152 mL/hr');
});

test('cats use the feline maintenance formula', async ({ page }) => {
  await open(page, 'fluids');
  await setPatient(page, '4', 'Cat');
  await expect(page.locator('#fluid-result .big')).toHaveText('9.43 mL/hr');
});

test('transfusion volume', async ({ page }) => {
  await open(page, 'transfusion');
  await setPatient(page, '20');
  await page.locator('#recipient').fill('15');
  await page.locator('#target').fill('25');

  await expect(page.locator('#transfusion-result .big')).toHaveText('450 mL');
});

test('BSA and RER', async ({ page }) => {
  await open(page, 'bsa');
  await setPatient(page, '10');

  await expect(page.locator('#bsa-result .big').first()).toHaveText('0.469 m²');
  await expect(page.locator('#energy-result .big')).toHaveText('RER 394 kcal/day');
});

test('unit converter', async ({ page }) => {
  await open(page, 'convert');
  await page.locator('#lb').fill('22');
  await expect(page.locator('#kg')).toHaveValue('9.98');
  await page.locator('#c').fill('38.5');
  await expect(page.locator('#f')).toHaveValue('101.3');
  await page.locator('#percent').fill('2');
  await expect(page.locator('#mgml')).toHaveValue('20');
});
