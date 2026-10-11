namespace ChocolateToxicity.Clinical.Tests;

/// <summary>Cases from the second maths audit.</summary>
public class MathAudit2Tests
{
    [Theory]
    [InlineData(0.25, 2)] // rounds to 0.5: +100%
    [InlineData(0.75, 2)] // rounds to 1: +33%
    [InlineData(0.24, 2)] // rounds to 0
    [InlineData(1.3, 2)] // rounds to 1.5: +15%
    public void Warns_when_splitting_changes_the_dose_by_more_than_10_percent(double tablets, int splitInto)
    {
        var rounded = DrugDose.RoundToSplit(tablets, splitInto);

        Assert.NotNull(DrugDose.SplitWarning(tablets, rounded));
    }

    [Theory]
    [InlineData(0.25, 4)]
    [InlineData(2.1, 2)] // rounds to 2: -5%
    [InlineData(1, 1)]
    public void No_warning_when_splitting_is_close(double tablets, int splitInto)
    {
        var rounded = DrugDose.RoundToSplit(tablets, splitInto);

        Assert.Null(DrugDose.SplitWarning(tablets, rounded));
    }

    [Fact]
    public void Split_warning_says_how_much_the_dose_changes()
    {
        Assert.Contains("+100%", DrugDose.SplitWarning(0.25, 0.5));
        Assert.Contains("smallest portion", DrugDose.SplitWarning(0.24, 0));
    }
}
