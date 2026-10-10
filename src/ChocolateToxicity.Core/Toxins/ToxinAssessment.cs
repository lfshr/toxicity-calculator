namespace ChocolateToxicity.Core.Toxins;

/// <summary>Outcome of a toxin dose calculation.</summary>
/// <param name="Profile">The toxin's thresholds.</param>
/// <param name="DosePerKg">Dose relative to the dog's weight, in the profile's dose unit.</param>
/// <param name="Band">The band the dose falls in.</param>
/// <param name="EmesisIndicated">
/// True when the dose reaches the point at which inducing emesis should be considered. Time since ingestion and the
/// patient's condition remain clinical decisions.
/// </param>
public sealed record ToxinAssessment(ToxinProfile Profile, double DosePerKg, DoseBand Band, bool EmesisIndicated);
