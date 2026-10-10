# Chocolate Toxicity Calculator

A calculator for veterinary surgeons that estimates the methylxanthine (theobromine plus caffeine) dose a dog
has eaten from chocolate, and whether inducing emesis should be considered. It is modelled on the
[Vets Now chocolate toxicity calculator](https://www.vets-now.com/dog-chocolate-toxicity-calculator/).

## Layout

- `src/ChocolateToxicity.Core`: the calculation library.
- `src/ChocolateToxicity.ViewModels`: the screens' logic (input parsing, validation, result wording), kept free of UI types so it can be tested on any machine.
- `src/ChocolateToxicity.App`: the .NET MAUI Android app.
- `tests/ChocolateToxicity.Core.Tests`: xUnit tests, including the Vets Now worked examples.
- `tests/ChocolateToxicity.ViewModels.Tests`: xUnit tests for the screens' logic.

Run the tests with `dotnet test tests/ChocolateToxicity.Core.Tests` and `dotnet test tests/ChocolateToxicity.ViewModels.Tests` (.NET 10 SDK).

### Building the Android app

You need the .NET 10 SDK, the MAUI Android workload, a JDK (17 or later) and the Android SDK:

```
dotnet workload install maui-android
dotnet build src/ChocolateToxicity.App -f net10.0-android -t:InstallAndroidDependencies -p:AndroidSdkDirectory=<android sdk path> -p:AcceptAndroidSDKLicenses=true
dotnet build src/ChocolateToxicity.App -f net10.0-android -c Release -p:AndroidSdkDirectory=<android sdk path>
```

The APK lands in `src/ChocolateToxicity.App/bin/Release/net10.0-android/`.

## The app

The calculator screen takes the dog's weight in kg, what was eaten (a chocolate type, or the cocoa percentage on the
label) and the amount eaten in grams. The result screen leads with the verdict (consider inducing emesis, or not
indicated by dose), coloured by severity, then the dose in mg/kg, the expected signs, the working and a clinical
disclaimer. Numbers can be typed with either a full stop or the device's decimal separator.

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
| Mild dark (semisweet, 35-50%) | 5.6 | Merck, upper value of 5.3-5.6 |
| Strong dark (over 50%) | 10.1 | Cocoa % curve at an assumed 70% |
| Baking (unsweetened) | 15.5 | Merck |
| Cocoa powder | 28.5 | Merck |
| Cocoa bean hulls (mulch) | 9.0 | Merck |

Merck gives combined theobromine and caffeine content. Where it gives a range, the upper value is used so the
calculator errs towards treating. For strong dark chocolate above about 70%, enter the cocoa percentage instead.

### From a cocoa percentage

Content is interpolated linearly between these points: 0% gives 0 mg/g, 30% (typical milk chocolate) gives 2.3,
45% (the bottom of Vets Now's 45-59% lower-grade dark band) gives 5.6, and 100% (unsweetened baking chocolate)
gives 15.5. This deliberately overestimates for most bars, because cocoa butter dilutes theobromine and the
percentage on a label counts it. It is not valid for cocoa powder, which is defatted and far stronger per gram.

### Severity bands

| Dose | Band | Expected signs |
|---|---|---|
| under 20 mg/kg | None | Methylxanthine toxicosis not expected; fat and sugar may still cause GI upset |
| 20 mg/kg | Mild | Vomiting, diarrhoea, polydipsia |
| 40 mg/kg | Severe | Cardiotoxic effects |
| 60 mg/kg | Seizures | Tremors and seizures |

Emesis is flagged for consideration from 20 mg/kg. Time since ingestion and the patient's condition remain clinical decisions.

## Sources

- Merck Veterinary Manual, [Chocolate Toxicosis in Animals](https://www.merckvetmanual.com/toxicology/food-hazards/chocolate-toxicosis-in-animals): methylxanthine content per type and the 20, 40-50 and 60 mg/kg thresholds.
- Vets Now, [My dog has eaten chocolate](https://www.vets-now.com/pet-care-advice/dogs/dog-eaten-chocolate): the same thresholds, theobromine figures for dark chocolate and cocoa powder, and the worked examples used in the tests.

## Disclaimer

This is a decision aid for qualified veterinary professionals, not a substitute for clinical judgement.
Individual dogs vary in sensitivity, and product content varies by brand and batch.
