namespace ChocolateToxicity.Core.Toxins;

/// <summary>A dose range and what to expect within it.</summary>
/// <param name="FromDose">Lowest dose in the band, per kg of body weight in the toxin's dose unit.</param>
/// <param name="Risk">How serious the band is.</param>
/// <param name="Title">Short heading, such as "Hypoglycaemia risk".</param>
/// <param name="Signs">Expected clinical signs or action in the band.</param>
public sealed record DoseBand(double FromDose, RiskLevel Risk, string Title, string Signs);
