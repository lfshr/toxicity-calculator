namespace ChocolateToxicity.Core.Toxins;

/// <summary>Human painkillers dogs commonly eat.</summary>
public enum Painkiller
{
    Ibuprofen,
    Paracetamol,
}

/// <summary>Ibuprofen and paracetamol toxicosis in dogs. Dose is mg of drug per kg.</summary>
/// <remarks>
/// Ibuprofen: Dunayer, "Ibuprofen toxicosis in dogs, cats, and ferrets", Veterinary Medicine 2004
/// (https://www.dvm360.com/view/toxciology-brief-ibuprofen-toxicosis-dogs-cats-and-ferrets): single doses as low as
/// 25 mg/kg can cause vomiting, the risk of acute renal failure rises above 175 mg/kg, CNS effects are seen above
/// 400 mg/kg and the minimum lethal dose is about 600 mg/kg. The Merck Veterinary Manual gives GI signs at 100-125 mg/kg,
/// renal failure at 175-300 mg/kg and CNS effects above 400 mg/kg.
/// Paracetamol: Merck Veterinary Manual, "Toxicoses From Human Analgesics in Animals"
/// (https://www.merckvetmanual.com/toxicology/toxicoses-from-human-analgesics/toxicoses-from-human-analgesics-in-animals):
/// clinical signs of acute toxicosis are not seen in dogs unless the dose exceeds 100 mg/kg, and methaemoglobinaemia
/// has been reported above 200 mg/kg; hepatotoxicity is more common in dogs than in cats.
/// </remarks>
public static class Painkillers
{
    public const double IbuprofenGiMgPerKg = 25;
    public const double IbuprofenRenalMgPerKg = 175;
    public const double IbuprofenCnsMgPerKg = 400;
    public const double IbuprofenMinimumLethalMgPerKg = 600;

    public const double ParacetamolSignsMgPerKg = 100;
    public const double ParacetamolMethaemoglobinaemiaMgPerKg = 200;

    public static ToxinProfile Ibuprofen { get; } = new(
        "ibuprofen",
        "mg/kg",
        new DoseBand(0, RiskLevel.Low, "Below toxic dose", "Clinical signs are not expected from a single dose below 25 mg/kg."),
        [
            new DoseBand(
                IbuprofenGiMgPerKg,
                RiskLevel.Moderate,
                "GI signs expected",
                "From 25 mg/kg: vomiting, diarrhoea and abdominal pain, with GI ulceration more likely as the dose rises."),
            new DoseBand(
                IbuprofenRenalMgPerKg,
                RiskLevel.High,
                "Kidney injury risk",
                "From 175 mg/kg: acute kidney injury, as well as GI ulceration."),
            new DoseBand(
                IbuprofenCnsMgPerKg,
                RiskLevel.Critical,
                "CNS signs possible",
                "From 400 mg/kg: seizures, ataxia, depression and coma, plus kidney and GI effects. The minimum lethal dose is about 600 mg/kg."),
        ],
        emesisFromDose: IbuprofenGiMgPerKg,
        "Thresholds: GI signs from 25 mg/kg, kidney injury from 175 mg/kg, CNS signs from 400 mg/kg, minimum lethal dose about 600 mg/kg " +
        "(ASPCA Animal Poison Control Center data; Merck Veterinary Manual).",
        "Consider gastroprotection as well as decontamination, and kidney monitoring from 175 mg/kg.");

    public static ToxinProfile Paracetamol { get; } = new(
        "paracetamol",
        "mg/kg",
        new DoseBand(0, RiskLevel.Low, "Below toxic dose", "Clinical signs are not expected below 100 mg/kg."),
        [
            new DoseBand(
                ParacetamolSignsMgPerKg,
                RiskLevel.High,
                "Liver injury risk",
                "From 100 mg/kg: vomiting, anorexia and hepatotoxicity, with icterus and hepatic necrosis."),
            new DoseBand(
                ParacetamolMethaemoglobinaemiaMgPerKg,
                RiskLevel.Critical,
                "Methaemoglobinaemia",
                "From 200 mg/kg: methaemoglobinaemia (brown mucous membranes, cyanosis, dyspnoea) and facial or paw oedema, plus liver injury."),
        ],
        emesisFromDose: ParacetamolSignsMgPerKg,
        "Thresholds: clinical signs and liver injury from 100 mg/kg, methaemoglobinaemia from 200 mg/kg (Merck Veterinary Manual).",
        "Consider acetylcysteine as well as decontamination at toxic doses.");

    public static ToxinProfile For(Painkiller drug) => drug switch
    {
        Painkiller.Ibuprofen => Ibuprofen,
        Painkiller.Paracetamol => Paracetamol,
        _ => throw new ArgumentOutOfRangeException(nameof(drug), drug, "Unknown painkiller."),
    };

    public static ToxinAssessment Assess(Painkiller drug, double totalMg, double dogWeightKg) =>
        For(drug).Assess(Dose.PerKg(totalMg, dogWeightKg));
}
