namespace ChocolateToxicity.Clinical;

/// <summary>Volume of blood to transfuse to reach a target PCV.</summary>
/// <remarks>
/// Merck Veterinary Manual, "Blood Transfusions in Dogs and Cats": volume = (desired PCV - recipient PCV) / donor unit PCV
/// x blood volume x body weight, with blood volume 80-90 mL/kg in dogs and 40-60 mL/kg in cats. The upper values
/// (90 and 60) are the defaults here, as is common practice.
/// </remarks>
public static class Transfusion
{
    public const double DogBloodVolumeMlPerKg = 90;
    public const double CatBloodVolumeMlPerKg = 60;

    public static double DefaultBloodVolumeMlPerKg(Species species) =>
        species == Species.Cat ? CatBloodVolumeMlPerKg : DogBloodVolumeMlPerKg;

    public static double VolumeMl(double weightKg, double recipientPcv, double targetPcv, double donorPcv, double bloodVolumeMlPerKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        Guard.NotNegative(recipientPcv, nameof(recipientPcv));
        Guard.Positive(donorPcv, nameof(donorPcv));
        Guard.Positive(bloodVolumeMlPerKg, nameof(bloodVolumeMlPerKg));
        if (targetPcv <= recipientPcv)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPcv), targetPcv, "Target PCV must be above the patient's PCV.");
        }

        if (targetPcv >= donorPcv)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPcv), targetPcv, "Target PCV must be below the donor unit's PCV.");
        }

        return (targetPcv - recipientPcv) / donorPcv * bloodVolumeMlPerKg * weightKg;
    }
}
