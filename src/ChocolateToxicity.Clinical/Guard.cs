namespace ChocolateToxicity.Clinical;

internal static class Guard
{
    public static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Must be greater than zero.");
        }
    }

    public static void NotNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Cannot be negative.");
        }
    }
}
