namespace ChocolateToxicity.Core.Toxins;

public static class Dose
{
    /// <summary>Divides the amount eaten by the dog's weight, giving a dose per kg in the amount's unit.</summary>
    public static double PerKg(double amount, double dogWeightKg)
    {
        if (double.IsNaN(amount) || amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount eaten cannot be negative.");
        }

        if (double.IsNaN(dogWeightKg) || dogWeightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dogWeightKg), dogWeightKg, "Dog weight must be greater than zero.");
        }

        return amount / dogWeightKg;
    }
}
