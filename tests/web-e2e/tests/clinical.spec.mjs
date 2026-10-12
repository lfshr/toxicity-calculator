import { test, expect } from '@playwright/test';
import { open, setPatient } from './helpers.mjs';

test('emetics for a 20 kg dog', async ({ page }) => {
  await open(page, 'emesis');
  await setPatient(page, '20');

  await expect(page.locator('#apomorphine-sc')).toHaveText('2 mg = 2 mL');
  await expect(page.locator('#clevor')).toHaveText('3 drops');
  await expect(page.locator('#charcoal')).toHaveText('20-40 g');
});

test('emesis is worthwhile for longer after chocolate, with the full contraindications', async ({ page }) => {
  await open(page, 'emesis');
  await setPatient(page, '20');
  await expect(page.getByText(/up to about 6 hours/)).toBeVisible();
  await expect(page.locator('#emesis-contraindications')).toContainText('hypoxic or dyspnoeic');
  await expect(page.locator('#emesis-contraindications')).toContainText('dopamine antagonist');
});

test('emetics for a 4 kg cat use dexmedetomidine', async ({ page }) => {
  await open(page, 'emesis');
  await setPatient(page, '4', 'Cat');
  await expect(page.locator('#dexmedetomidine-im')).toHaveText('28-160 µg = 0.056-0.32 mL');
  await expect(page.locator('#dexmedetomidine-iv')).toHaveText('14 µg = 0.028 mL');
  await expect(page.locator('#apomorphine-sc')).toHaveCount(0);
  await expect(page.locator('#charcoal')).toHaveText('4-8 g');
});

test('CPR vasopressors follow RECOVER 2024', async ({ page }) => {
  await open(page, 'emergency');
  await setPatient(page, '10');
  await expect(page.getByText(/VF or pulseless VT after a failed shock: suggested first-line vasopressor/)).toBeVisible();
  await expect(page.getByText(/Shockable rhythms: after a failed shock, if vasopressin is unavailable/)).toBeVisible();
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
  // RECOVER 2024: lidocaine first in dogs, amiodarone if lidocaine is unavailable.
  await expect(page.locator('#cpr-table td strong').filter({ hasText: /^(Lidocaine|Amiodarone)$/ })).toHaveText(['Lidocaine', 'Amiodarone']);
  await expect(page.locator('#defib')).toHaveText('External: first shock 20 J, later shocks 40 J');
  await expect(page.getByText('RECOVER 2024 doubles the energy once, to 4 J/kg')).toBeVisible();
  await expect(page.getByText('50%')).toHaveCount(0);
  await page.getByRole('radio', { name: 'Monophasic' }).click();
  await expect(page.locator('#defib')).toHaveText('External: first shock 40 J, later shocks 60 J');
  await expect(page.getByText('a choice within the range, not a RECOVER rule')).toBeVisible();
  await expect(row('Amiodarone')).toContainText('Avoid polysorbate-80 formulations in dogs');
});

test('CPR drugs for a cat leave out lidocaine', async ({ page }) => {
  await open(page, 'emergency');
  await setPatient(page, '4', 'Cat');

  await expect(page.locator('#cpr-table td strong', { hasText: 'Lidocaine' })).toHaveCount(0);
  await expect(page.locator('#cpr-table')).toContainText('preferred in cats');
  await expect(page.locator('#cpr-table')).not.toContainText('polysorbate');
  await expect(page.locator('#lidocaine-cat')).toBeVisible();
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
  await expect(page.getByText('Usually 12-24 hours (AAHA 2024)')).toBeVisible();
  await expect(page.getByText('this calculator follows AAHA')).toBeVisible();
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
  await expect(page.getByText('Often 25-30%')).toHaveCount(0);
  await expect(page.locator('#transfusion-result')).toContainText('0.25 mL/kg over 15 minutes');
  await expect(page.locator('#transfusion-result')).toContainText('more than 4 days ago (Merck Veterinary Manual)');
  await expect(page.getByText('first 15-30 minutes')).toHaveCount(0);
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

test('drug dose warns when splitting tablets changes the dose a lot', async ({ page }) => {
  await open(page, 'dose');
  await setPatient(page, '5');
  await page.locator('#dose').fill('5');
  await page.locator('#tablet').fill('100');

  // 25 mg is a quarter tablet: rounding to halves gives 0.5 tablets, double the dose.
  const result = page.locator('#dose-result');
  await expect(result).toContainText('0.5 tablets');
  await expect(page.locator('#split-warning')).toContainText('+100%');

  await page.locator('#split').selectOption('4');
  await expect(page.locator('#split-warning')).toHaveCount(0);
});
