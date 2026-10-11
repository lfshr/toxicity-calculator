# Toxicity Calculator

A calculator for veterinary surgeons that estimates the dose of a toxin a dog has eaten, and whether inducing emesis
should be considered. It covers chocolate, xylitol, ibuprofen, paracetamol and rat and mouse bait.


It comes as an Android app and as a web app. The web app adds clinical calculators for dogs and cats: emetics and
activated charcoal, CPR drugs and defibrillation, drug doses, constant rate infusions, fluid therapy, blood
transfusion, body surface area and energy requirements, and unit conversions.

## Layout

- `src/ChocolateToxicity.Core`: the calculation library. The other toxins are in `Toxins/`.
- `src/ChocolateToxicity.ProductSearch`: product search against Open Food Facts.
- `src/ChocolateToxicity.ViewModels`: the screens' logic (input parsing, validation, result wording), kept free of UI types so it can be tested on any machine.
- `src/ChocolateToxicity.App`: the .NET MAUI Android app.
- `src/ChocolateToxicity.Clinical`: the web app's clinical calculations (emetics, CPR drugs, doses, CRI, fluids, transfusion, BSA, energy, units).
- `src/ChocolateToxicity.Web`: the Blazor WebAssembly web app. It runs entirely in the browser and reuses the libraries above.
- `tests/ChocolateToxicity.Core.Tests`: xUnit tests, including the Vets Now worked examples.
- `tests/ChocolateToxicity.ProductSearch.Tests`: xUnit tests for reading Open Food Facts results.
- `tests/ChocolateToxicity.ViewModels.Tests`: xUnit tests for the screens' logic.
- `tests/ChocolateToxicity.Clinical.Tests`: xUnit tests for the clinical calculations.
- `tests/web-e2e`: Playwright tests that drive the web app in Chromium at desktop and phone sizes.

Run the tests with `dotnet test tests/<project>` for each test project (.NET 10 SDK).

### Running the web app

```
dotnet run --project src/ChocolateToxicity.Web
```

To run the browser tests, publish the app and run Playwright (Node 20 or later):

```
dotnet publish src/ChocolateToxicity.Web -c Release -o publish/web
cd tests/web-e2e
npm ci
npx playwright install chromium
npx playwright test
```

The tests answer Open Food Facts requests from fixtures in `tests/web-e2e/fixtures`, so they run offline.

The "Deploy web app" GitHub Actions workflow publishes the app to GitHub Pages on every push to `main`, once Pages
is turned on (Settings > Pages > Source: GitHub Actions). The app is a static site, so any static host works:
serve `publish/web/wwwroot`, sending `index.html` for unknown paths. It can be installed to a phone's home screen
and works offline after the first visit. When online, the page always comes from the server, so a new deploy shows on the next normal
reload; the offline copy is only used without a connection. If a deploy lands while the app is open, a bar offers a
reload. `tests/web-e2e/tests/update.spec.mjs` checks this by deploying one build and then another.

### Building the Android app

You need the .NET 10 SDK, the MAUI Android workload, a JDK (17 or later) and the Android SDK:

```
dotnet workload install maui-android
dotnet build src/ChocolateToxicity.App -f net10.0-android -t:InstallAndroidDependencies -p:AndroidSdkDirectory=<android sdk path> -p:AcceptAndroidSDKLicenses=true
dotnet build src/ChocolateToxicity.App -f net10.0-android -c Release -p:AndroidSdkDirectory=<android sdk path>
```

The APK lands in `src/ChocolateToxicity.App/bin/Release/net10.0-android/`.

### Signed release APK

Release builds are signed with the release key when `ANDROID_SIGNING_KEYSTORE` (the keystore's path) and
`ANDROID_SIGNING_PASSWORD` are set, and with the debug key otherwise:

```
ANDROID_SIGNING_KEYSTORE=/path/to/chocolatetoxicity.keystore ANDROID_SIGNING_PASSWORD=... \
  dotnet publish src/ChocolateToxicity.App -f net10.0-android -c Release -p:AndroidSdkDirectory=<android sdk path>
```

The signed APK is `publish/com.lfshr.chocolatetoxicity-Signed.apk` in the output folder. Every update must be
signed with the same key, or Android refuses to install it over the existing app, so keep the keystore and its
password backed up and out of the repository.

The "Release APK" GitHub Actions workflow builds the same APK when run by hand or on a `v*` tag, once the repository
has two secrets: `ANDROID_KEYSTORE_BASE64` (the keystore, base64-encoded) and `ANDROID_KEYSTORE_PASSWORD`.

### Installing on a device

Copy the signed APK to the phone or tablet (email, USB or a shared drive), open it, and allow installs from that
source when Android asks. To update, install the newer APK the same way; the app's data is kept.

## The app

The start screen lists the calculators. Each one takes the dog's weight and what was eaten, and leads to the same
result screen: the verdict coloured by risk, the dose, the expected signs, treatment notes, the working, the
thresholds with their source and a clinical disclaimer.

### Chocolate

The calculator screen takes the dog's weight in kg, what was eaten (a chocolate type, or the cocoa percentage on the
label) and the amount eaten in grams. The result screen leads with the verdict (consider inducing emesis, or not
indicated by dose), coloured by severity, then the dose in mg/kg, the expected signs, the working and a clinical
disclaimer. Numbers can be typed with either a full stop or the device's decimal separator.

### Product search

"Search products" looks the product up on [Open Food Facts](https://world.openfoodfacts.org), a free, open
product database, limited to cocoa products so "Dairy Milk" finds the chocolate rather than milk. The Android app
searches Open Food Facts' Search-a-licious service first; the web app can't, because that service doesn't allow
requests from other websites (no CORS headers), so it goes straight to the older search. The older search is often
busy and answers 503, so it is tried three times a few seconds apart, and if it is still busy the query is looked up
as a brand instead ("Lindt 70" as Lindt bars, listing those with 70 in the name first). The chosen product's full
record is then fetched for its cocoa data. The "Live product search" workflow runs real searches in a browser
against the live service; run it from the Actions tab. The chocolate is filled in from:

1. The cocoa percentage recorded from the label (Open Food Facts' "cocoa" nutriment), if there is one.
2. Otherwise a percentage in the product name, such as "Excellence 70% Cacao", but only when the name or category
   says it's dark chocolate or cocoa, so "30% less sugar" isn't read as cocoa. A percentage next to a cocoa word wins
   ("Noir 70% aux noisettes 15%" gives 70), and a figure under 45% in a dark chocolate's name ("with 20% hazelnuts")
   is ignored in favour of the dark type.
3. Otherwise the type its category implies: milk, white, cocoa powder, or strong dark for a dark bar with no
   percentage, which errs towards treating.

A product whose name or category says cocoa powder ("cacao powder", "cacao en poudre", "Kakaopulver") goes by the
cocoa powder type even without Open Food Facts' powder category. A product recorded as 100% cocoa could be a bar
or a powder, so the app asks you to choose the type rather than assume the weaker one.

Results whose cocoa content is unknown are left out of the list, and each result shows the product's photo when
Open Food Facts has one.

Cocoa powder and white chocolate always go by type: a percentage would understate defatted powder, and white
chocolate's cocoa is cocoa butter, with almost no theobromine. When the product's pack weight is known, the app
offers it as the amount eaten. Open Food Facts allows 10 searches a minute per device, so search runs when you
press search rather than as you type.

## How it calculates

```
dose (mg/kg) = methylxanthine content (mg/g) x amount eaten (g) / dog weight (kg)
```

The chocolate's content comes from either a chocolate type or the cocoa percentage on the label.

### Methylxanthine content by type

| Type | mg/g | Source |
|---|---|---|
| White | 0.04 | Merck |
| Milk | 2.3 | Merck |
| Mild dark (semisweet, 35-50%) | 6.5 | Cocoa % curve at 50%, the top of the band (Merck semisweet is 5.3-5.6) |
| Dark over 50%, % unknown | 10.1 | Cocoa % curve at an assumed 70% |
| Baking (unsweetened) | 15.5 | Merck |
| Cocoa powder | 28.5 | Merck |
| Cocoa bean hulls (mulch) | 9.0 | Merck |

Merck gives combined theobromine and caffeine content. Where it gives a range, the upper value is used so the
calculator errs towards treating. Dark chocolate over 50% with no known percentage is worked out at 70%, and the
picker says so; for a darker bar, enter the cocoa percentage instead (an 85% bar gives 12.8 mg/g).

### From a cocoa percentage

Content is interpolated linearly between these points: 0-20% gives 2.3 mg/g (the Merck milk chocolate
figure, placed at the lowest cocoa content UK and EU rules allow for milk chocolate, and held flat below it because
US milk chocolate can be 10%, so every bar with a stated cocoa % gets at least 2.3; white chocolate goes by type), 45% (the bottom of Vets Now's 45-59% lower-grade dark band) gives 5.6, and 100% (unsweetened baking chocolate)
gives 15.5. This deliberately overestimates for most bars, because cocoa butter dilutes theobromine and the
percentage on a label counts it. It is not valid for cocoa powder, which is defatted and far stronger per gram.

### Severity bands

| Dose | Band | Expected signs |
|---|---|---|
| under 20 mg/kg | None | Methylxanthine toxicosis not expected; fat and sugar may still cause GI upset |
| 20 mg/kg | Mild | Vomiting, diarrhoea, polydipsia |
| 40 mg/kg | Severe | Cardiotoxic effects |
| 60 mg/kg | Seizures | Tremors and seizures; potentially lethal from 100 mg/kg (Merck oral LD50 100-200 mg/kg) |

Emesis is flagged for consideration from 20 mg/kg. Time since ingestion and the patient's condition remain clinical decisions.

## Other toxins

Each toxin's dose is the amount eaten divided by the dog's weight, then compared with published dog thresholds.
Emesis is flagged from the first threshold that calls for treatment.

### Xylitol

Dose in mg/kg of xylitol, from the product eaten and the xylitol per 100 g on its label, or from a known amount.

| Dose | Expected |
|---|---|
| under 100 mg/kg | Hypoglycaemia not expected |
| 100 mg/kg | Hypoglycaemia (emesis flagged) |
| 500 mg/kg | Hepatic failure |

### Ibuprofen and paracetamol

Dose in mg/kg, from the strength per tablet (or mg per mL for a liquid) and the number eaten. The strength starts at
200 mg for ibuprofen and 500 mg for paracetamol.

| Ibuprofen | Expected |
|---|---|
| 25 mg/kg | Vomiting and GI signs (emesis flagged) |
| 175 mg/kg | Acute kidney injury |
| 400 mg/kg | CNS signs; minimum lethal dose about 600 mg/kg |

| Paracetamol | Expected |
|---|---|
| 100 mg/kg | Clinical signs and hepatotoxicity (emesis flagged) |
| 200 mg/kg | Methaemoglobinaemia |

### Rat and mouse bait

Dose in mg/kg of active ingredient, from the bait eaten and its concentration, which is filled in with the usual
strength for the chosen ingredient.

For anticoagulants the toxic dose is one-tenth of the lowest reported dog LD50, as the Merck Veterinary Manual
describes. Emesis is flagged from 0.02 mg/kg, the conservative decontamination threshold Merola recommends, or from
the toxic dose if that is lower.

| Ingredient | Usual bait | Lowest dog LD50 (mg/kg) | Toxic from (mg/kg) |
|---|---|---|---|
| Brodifacoum | 0.005% | 0.2 | 0.02 |
| Bromadiolone | 0.005% | 11 | 1.1 |
| Difenacoum | 0.005% | Not verified: assessed as flocoumafen, 0.075 | 0.0075 |
| Difethialone | 0.0025% | 4 | 0.4 |
| Flocoumafen | 0.005% | 0.075 | 0.0075 |
| Diphacinone | 0.005% | 0.9 | 0.09 |
| Warfarin | 0.025% | 20 | 2 |
| Unknown anticoagulant | 0.005% | 0.075 (as flocoumafen) | 0.0075 |

Cholecalciferol (vitamin D3, usually 0.075%): signs from 0.1 mg/kg (emesis flagged), soft tissue mineralisation
above 0.5 mg/kg.

For cats, the same one-tenth rule uses the cat LD50, but only where two published sources agree on it: warfarin
(lowest cat LD50 5 mg/kg, so toxic from 0.5 mg/kg) and diphacinone (15 mg/kg, toxic from 1.5 mg/kg). Merola 2002 and
Moorman 2002 disagree on brodifacoum and bromadiolone (unknown vs 25 mg/kg), only Merola gives difethialone (and only as
more than 16 mg/kg), and there is no published cat threshold for cholecalciferol, so those use the dog figures and the
page says feline data is lacking. The 0.02 mg/kg decontamination threshold is kept for cats.

## Clinical calculators (web app)

The patient's species and weight are entered at the top of each calculator and carried to the others.

| Calculator | Formula | Source |
|---|---|---|
| Apomorphine | 0.1 mg/kg SC of 1 mg/mL; 0.03-0.04 mg/kg IV | Emedog UK SPC; Merck |
| Ropinirole (Clevor) | Drops by weight band, 1.8-100 kg | Clevor UK SPC |
| Activated charcoal | 1-2 g/kg PO once | Merck, chocolate toxicosis |
| CPR drugs | Per-kg doses at an editable concentration; defibrillation 4-6 J/kg monophasic, 2-4 J/kg biphasic | RECOVER 2024 |
| Drug dose | mg = kg x mg/kg; mL = mg / (mg/mL); tablets rounded to the chosen split | |
| CRI | Undiluted mL/hr = mg/hr / (mg/mL); in fluids, mg to add = mg/hr x volume / rate | |
| Fluid therapy | Maintenance 132 x kg^0.75 (dog) or 80 x kg^0.75 (cat) mL/day; deficit = % x kg x 1000 mL | AAHA 2013; same formulas in AAHA's current maintenance rate table |
| Transfusion | (target - patient PCV) / donor PCV x blood volume (90 dog, 60 cat mL/kg) x kg | Merck |
| BSA | K x (g)^(2/3) x 10^-4, K = 10.1 dog, 10.0 cat | Merck |
| Energy | RER = 70 x kg^0.75 kcal/day, with life-stage multiples | WSAVA |

The chocolate and xylitol calculators use dog thresholds; no published source gives cat thresholds for chocolate, and
Merck says cats are not at risk from xylitol. Ibuprofen, paracetamol, warfarin and diphacinone use cat thresholds for a
cat. Wherever the dog figures are used for a cat, the web app says feline data is lacking.

## Sources

- Merck Veterinary Manual, [Chocolate Toxicosis in Animals](https://www.merckvetmanual.com/toxicology/food-hazards/chocolate-toxicosis-in-animals): methylxanthine content per type and the 20, 40-50 and 60 mg/kg thresholds.
- Merck Veterinary Manual, [Xylitol Toxicosis in Dogs](https://www.merckvetmanual.com/toxicology/food-hazards/xylitol-toxicosis-in-dogs): 100 and 500 mg/kg thresholds.
- Dunayer, [Ibuprofen toxicosis in dogs, cats, and ferrets](https://www.dvm360.com/view/toxciology-brief-ibuprofen-toxicosis-dogs-cats-and-ferrets), Veterinary Medicine 2004: ibuprofen thresholds from ASPCA Animal Poison Control Center data.
- Merck Veterinary Manual, [Toxicoses From Human Analgesics in Animals](https://www.merckvetmanual.com/toxicology/toxicoses-from-human-analgesics/toxicoses-from-human-analgesics-in-animals): ibuprofen and paracetamol thresholds in dogs.
- Merck Veterinary Manual, [Anticoagulant Rodenticide Poisoning in Animals](https://www.merckvetmanual.com/toxicology/rodenticide-poisoning/anticoagulant-rodenticide-poisoning-in-animals): toxic dose as one-tenth of the lowest LD50, decontamination and vitamin K1.
- Merola, [Anticoagulant rodenticides: Deadly for pests, dangerous for pets](https://www.aspcapro.org/sites/default/files/d-dbriefoct2002.pdf), Veterinary Medicine 2002: dog and cat LD50s, bait concentrations and the 0.02 mg/kg decontamination threshold.
- Moorman, [Anticoagulant rodenticides: now more toxic to pests and pets](https://www.aspcapro.org/sites/default/files/c-vettech_0102.pdf), Veterinary Technician 2002: cat LD50s, used where they agree with Merola.
- VPIS for BASF, [Emergency treatment of cholecalciferol rodenticide poisoning](https://www.vpisglobal.com/wp-content/uploads/2021/04/Vitamin-D-toxicosis-for-vets-paper-FINAL.pdf), 2020: the experimental cat study quoted in the cat warning.
- Animal Poisons Helpline, [Flocoumafen](https://www.animalpoisons.com.au/alerts/flocoumafen/): flocoumafen dog LD50. No verifiable dog LD50 was found for difenacoum (the 50 mg/kg sometimes quoted couldn't be
  traced to a primary source), so it is assessed like an unknown anticoagulant.
- Merck Veterinary Manual, [Cholecalciferol (Vitamin D3) Poisoning in Animals](https://www.merckvetmanual.com/toxicology/rodenticide-poisoning/cholecalciferol-vitamin-d3-poisoning-in-animals): 0.1 and 0.5 mg/kg thresholds and bait concentration.
- [RECOVER 2024 drug dosing chart](https://recoverinitiative.org/wp-content/uploads/2026/01/RECOVER_2024_Drug_Dosing_Charts_KGS.pdf): CPR drug doses, concentrations and defibrillation energies.
- VMD, [Clevor SPC](https://www.vmd.defra.gov.uk/productInformationdatabase/files/SPC_Documents/SPC_3016241.PDF): ropinirole drops by weight. [Emedog SPC](https://www.vmd.defra.gov.uk/productInformationdatabase/files/SPC_Documents/SPC_2419253.PDF): apomorphine 0.1 mg/kg SC.
- AAHA, [Fluids for maintenance and replacement](https://www.aaha.org/aaha-guidelines/fluid-therapy/fluids-for-maintenance-and-replacement/) (2013 guidelines) and [Maintenance fluid rates](https://aaha.org/aaha-guidelines/fluid-therapy/quick-reference-fluid-tables/maintenance-fluid-rates/): maintenance formulas.
- Merck Veterinary Manual, [Blood Transfusions in Dogs and Cats](https://www.merckvetmanual.com/circulatory-system/blood-groups-and-blood-transfusions-in-dogs-and-cats/blood-transfusions-in-dogs-and-cats) and [weight to BSA conversion](https://www.merckvetmanual.com/multimedia/table/weight-to-body-surface-area-conversion-for-dogs).
- Vets Now, [My dog has eaten chocolate](https://www.vets-now.com/pet-care-advice/dogs/dog-eaten-chocolate): the same thresholds, theobromine figures for dark chocolate and cocoa powder, and the worked examples used in the tests.

## Disclaimer

This is a decision aid for qualified veterinary professionals, not a substitute for clinical judgement.
Individual dogs vary in sensitivity, and product content varies by brand and batch. Where no published cat figure
exists, the dog thresholds are shown for cats as a minimum estimate.
