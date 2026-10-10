namespace ChocolateToxicity.Core.Toxins;

/// <summary>Fresh grapes, or dried grapes (raisins, sultanas and Zante currants).</summary>
public enum GrapeForm
{
    Grapes,
    Dried,
}

/// <summary>Grape and raisin toxicosis in dogs. Dose is grams of fruit per kg.</summary>
/// <remarks>
/// There is no established safe dose and susceptibility is unpredictable, so any ingestion warrants decontamination.
/// Merck Veterinary Manual, "Grape, Raisin, and Tamarind Toxicosis in Dogs"
/// (https://www.merckvetmanual.com/toxicology/food-hazards/raisins-and-grapes): prompt GI decontamination is the
/// recommended treatment, and Zante currants are implicated but true (Ribes) currants are not.
/// Vet Times, "Canine grape toxicosis" (https://www.vettimes.com/news/vets/small-animal-vets/canine-grape-toxicosis):
/// the lowest reported doses to cause acute kidney injury are 19.6 g/kg for grapes and 2.8 g/kg for raisins.
/// </remarks>
public static class GrapesAndRaisins
{
    /// <summary>Lowest reported dose of grapes to cause acute kidney injury, in g/kg.</summary>
    public const double GrapesLowestReportedAkiGPerKg = 19.6;

    /// <summary>Lowest reported dose of raisins to cause acute kidney injury, in g/kg.</summary>
    public const double DriedLowestReportedAkiGPerKg = 2.8;

    private const string Advice =
        "Induce emesis in an asymptomatic dog even several hours after ingestion, since grapes can stay in the stomach " +
        "for a long time. After a large ingestion, or if vomiting or diarrhoea start, give IV fluids for at least 48 hours " +
        "and monitor renal function.";

    public static ToxinProfile For(GrapeForm form) => form switch
    {
        GrapeForm.Grapes => Grapes,
        GrapeForm.Dried => Dried,
        _ => throw new ArgumentOutOfRangeException(nameof(form), form, "Unknown grape form."),
    };

    public static ToxinProfile Grapes { get; } = Create("grapes", GrapesLowestReportedAkiGPerKg);

    public static ToxinProfile Dried { get; } = Create("raisins", DriedLowestReportedAkiGPerKg);

    public static ToxinAssessment Assess(GrapeForm form, double amountEatenGrams, double dogWeightKg) =>
        For(form).Assess(Dose.PerKg(amountEatenGrams, dogWeightKg));

    private static ToxinProfile Create(string name, double lowestReportedAki) => new(
        name,
        "g/kg",
        new DoseBand(
            0,
            RiskLevel.High,
            "No safe dose",
            "Kidney injury has no established safe dose and some dogs react to a few grapes. Treat any ingestion."),
        [
            new DoseBand(
                lowestReportedAki,
                RiskLevel.Critical,
                "Above lowest reported kidney injury dose",
                $"At or above {lowestReportedAki:0.0} g/kg, the lowest dose of {name} reported to cause acute kidney injury: " +
                "vomiting, lethargy and diarrhoea, then oliguria or anuria."),
        ],
        emesisFromDose: 0,
        $"No safe dose is established. Lowest reported doses causing kidney injury: grapes {GrapesLowestReportedAkiGPerKg:0.0} g/kg, " +
        $"raisins {DriedLowestReportedAkiGPerKg:0.0} g/kg (Vet Times). Merck Veterinary Manual recommends prompt decontamination.",
        Advice);
}
