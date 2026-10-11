namespace ChocolateToxicity.Core.Toxins;

/// <summary>Active ingredients of rat and mouse bait.</summary>
public enum RodenticideIngredient
{
    Brodifacoum,
    Bromadiolone,
    Difenacoum,
    Difethialone,
    Flocoumafen,
    Diphacinone,
    Warfarin,

    /// <summary>An anticoagulant whose name isn't known; assessed as the most potent listed, flocoumafen.</summary>
    UnknownAnticoagulant,

    /// <summary>Vitamin D3.</summary>
    Cholecalciferol,
}

/// <summary>An active ingredient with its usual bait concentration and, for anticoagulants, its lowest reported dog LD50.</summary>
/// <param name="Name">Name for use mid-sentence.</param>
/// <param name="DefaultConcentrationPercent">Usual concentration in bait, as a percentage by weight.</param>
/// <param name="LowestDogLd50MgPerKg">
/// Lowest reported oral LD50 in dogs, for anticoagulants; null for cholecalciferol. For an anticoagulant without a
/// verified dog LD50 this is flocoumafen's, the most potent listed, and <paramref name="Ld50Verified"/> is false.
/// </param>
/// <param name="Ld50Verified">False when the ingredient has no verified dog LD50 and is assessed as flocoumafen.</param>
public sealed record RodenticideInfo(
    RodenticideIngredient Ingredient,
    string Name,
    double DefaultConcentrationPercent,
    double? LowestDogLd50MgPerKg,
    bool Ld50Verified = true);

/// <summary>Rat and mouse bait toxicosis in dogs. Dose is mg of active ingredient per kg.</summary>
/// <remarks>
/// Anticoagulants: Merck Veterinary Manual, "Anticoagulant Rodenticide Poisoning in Animals"
/// (https://www.merckvetmanual.com/toxicology/rodenticide-poisoning/anticoagulant-rodenticide-poisoning-in-animals):
/// toxic doses are based on one-tenth of the lowest reported LD50 for the ingredient and species. Dog LD50s and bait
/// concentrations from Merola, "Anticoagulant rodenticides: Deadly for pests, dangerous for pets", Veterinary Medicine 2002
/// (https://www.aspcapro.org/sites/default/files/d-dbriefoct2002.pdf), which also recommends decontaminating and monitoring
/// any dog that eats more than 0.02 mg/kg. Flocoumafen LD50 from Animal Poisons Helpline
/// (https://www.animalpoisons.com.au/alerts/flocoumafen/). No verifiable dog LD50 was found for difenacoum (Merola doesn't
/// list it, and the 50 mg/kg sometimes quoted could not be traced to a primary source), so it is assessed like an
/// unknown anticoagulant, as flocoumafen.
/// Cholecalciferol: Merck Veterinary Manual, "Cholecalciferol (Vitamin D3) Poisoning in Animals"
/// (https://www.merckvetmanual.com/toxicology/rodenticide-poisoning/cholecalciferol-vitamin-d3-poisoning-in-animals):
/// signs from 0.1 mg/kg, soft tissue mineralisation above 0.5 mg/kg, baits usually 0.075%.
/// </remarks>
public static class Rodenticides
{
    /// <summary>Merola's conservative dose above which any anticoagulant ingestion is decontaminated and monitored.</summary>
    public const double AnticoagulantDecontaminateMgPerKg = 0.02;

    /// <summary>The most potent anticoagulant listed, used for those without a verified dog LD50.</summary>
    public const double FlocoumafenLd50MgPerKg = 0.075;

    public const double CholecalciferolSignsMgPerKg = 0.1;
    public const double CholecalciferolMineralisationMgPerKg = 0.5;

    public static IReadOnlyList<RodenticideInfo> All { get; } =
    [
        new(RodenticideIngredient.Brodifacoum, "brodifacoum", 0.005, 0.2),
        new(RodenticideIngredient.Bromadiolone, "bromadiolone", 0.005, 11),
        new(RodenticideIngredient.Difenacoum, "difenacoum", 0.005, FlocoumafenLd50MgPerKg, Ld50Verified: false),
        new(RodenticideIngredient.Difethialone, "difethialone", 0.0025, 4),
        new(RodenticideIngredient.Flocoumafen, "flocoumafen", 0.005, FlocoumafenLd50MgPerKg),
        new(RodenticideIngredient.Diphacinone, "diphacinone", 0.005, 0.9),
        new(RodenticideIngredient.Warfarin, "warfarin", 0.025, 20),
        new(RodenticideIngredient.UnknownAnticoagulant, "an unknown anticoagulant", 0.005, FlocoumafenLd50MgPerKg, Ld50Verified: false),
        new(RodenticideIngredient.Cholecalciferol, "cholecalciferol", 0.075, null),
    ];

    public static RodenticideInfo Info(RodenticideIngredient ingredient) => All.Single(i => i.Ingredient == ingredient);

    /// <summary>Active ingredient eaten, in mg, from the bait eaten and its concentration.</summary>
    public static double ActiveMgEaten(double baitEatenGrams, double concentrationPercent)
    {
        if (double.IsNaN(baitEatenGrams) || baitEatenGrams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baitEatenGrams), baitEatenGrams, "Amount eaten cannot be negative.");
        }

        if (double.IsNaN(concentrationPercent) || concentrationPercent <= 0 || concentrationPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(concentrationPercent), concentrationPercent, "Concentration must be above 0% and at most 100%.");
        }

        return baitEatenGrams * concentrationPercent / 100 * 1000;
    }

    public static ToxinAssessment Assess(RodenticideIngredient ingredient, double activeMg, double dogWeightKg) =>
        ProfileFor(ingredient).Assess(Dose.PerKg(activeMg, dogWeightKg));

    public static ToxinProfile ProfileFor(RodenticideIngredient ingredient)
    {
        var info = Info(ingredient);
        return info.LowestDogLd50MgPerKg is { } ld50 ? Anticoagulant(info.Name, ld50, info.Ld50Verified) : Cholecalciferol;
    }

    public static ToxinProfile Cholecalciferol { get; } = new(
        "cholecalciferol",
        "mg/kg",
        new DoseBand(0, RiskLevel.Low, "Below toxic dose", "Clinical signs are not expected below 0.1 mg/kg."),
        [
            new DoseBand(
                CholecalciferolSignsMgPerKg,
                RiskLevel.High,
                "Hypercalcaemia risk",
                "From 0.1 mg/kg: hypercalcaemia and hyperphosphataemia, with vomiting, polyuria, polydipsia and lethargy, usually 12-48 hours after ingestion."),
            new DoseBand(
                CholecalciferolMineralisationMgPerKg,
                RiskLevel.Critical,
                "Mineralisation and kidney injury",
                "From 0.5 mg/kg: soft tissue mineralisation and acute kidney injury."),
        ],
        emesisFromDose: CholecalciferolSignsMgPerKg,
        "Thresholds: signs from 0.1 mg/kg, soft tissue mineralisation above 0.5 mg/kg (Merck Veterinary Manual).",
        "Induce emesis within 4 hours of ingestion. Merck advises activated charcoal for 0.1-0.5 mg/kg, and charcoal then " +
        "cholestyramine above 0.5 mg/kg (check the cholestyramine product is xylitol-free). Monitor calcium, phosphorus and renal values.");

    private static ToxinProfile Anticoagulant(string name, double lowestLd50, bool ld50Verified)
    {
        var ld50Text = ld50Verified
            ? $"the lowest reported dog LD50 for {name}"
            : $"the flocoumafen dog LD50, used because there is no verified dog LD50 for {name}";
        var toxicDose = lowestLd50 / 10;
        var bands = new List<DoseBand>();
        if (toxicDose > AnticoagulantDecontaminateMgPerKg)
        {
            bands.Add(new DoseBand(
                AnticoagulantDecontaminateMgPerKg,
                RiskLevel.Moderate,
                "Decontaminate and monitor",
                $"From 0.02 mg/kg: below the toxic dose for {name}, but some dogs are sensitive. Decontaminate, then check PT at 48 and 72 hours."));
        }

        bands.Add(new DoseBand(
            toxicDose,
            RiskLevel.High,
            "Toxic dose",
            $"From {FormatMg(toxicDose)} mg/kg (one-tenth of {ld50Text}): coagulopathy is expected without treatment. " +
            "Bleeding usually starts several days after ingestion."));
        bands.Add(new DoseBand(
            lowestLd50,
            RiskLevel.Critical,
            "Potentially lethal dose",
            $"From {FormatMg(lowestLd50)} mg/kg, {ld50Text}: severe coagulopathy and haemorrhage are likely without vitamin K1."));

        var decontaminateFrom = Math.Min(toxicDose, AnticoagulantDecontaminateMgPerKg);
        return new ToxinProfile(
            name,
            "mg/kg",
            new DoseBand(
                0,
                RiskLevel.Low,
                "Below toxic dose",
                $"Below {FormatMg(decontaminateFrom)} mg/kg. If the amount eaten is uncertain, decontaminate anyway or check PT 48 hours after ingestion."),
            bands,
            emesisFromDose: decontaminateFrom,
            $"Thresholds for {name}: decontaminate from {FormatMg(decontaminateFrom)} mg/kg (Merola 2002), toxic from " +
            $"{FormatMg(toxicDose)} mg/kg (one-tenth of the lowest dog LD50, Merck Veterinary Manual), " +
            (ld50Verified
                ? $"lowest dog LD50 {FormatMg(lowestLd50)} mg/kg."
                : $"assessed as flocoumafen (dog LD50 {FormatMg(lowestLd50)} mg/kg) because no verified dog LD50 for {name} was found."),
            "Induce emesis within 4 hours of ingestion. At toxic doses, give vitamin K1 (Merck: 2.5 mg/kg PO every 12 hours or " +
            "5 mg/kg every 24 hours, with food) for 4 weeks for second-generation baits, and check PT 48-72 hours after the last dose.");
    }

    private static string FormatMg(double value) => value.ToString("0.####", System.Globalization.CultureInfo.CurrentCulture);
}
