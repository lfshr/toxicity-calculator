using System.Globalization;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels.Tests;

public class ResultViewModelTests
{
    public ResultViewModelTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
    }

    [Theory]
    [InlineData(10, SeverityBand.None, "Below toxic dose")]
    [InlineData(25, SeverityBand.Mild, "Mild signs expected")]
    [InlineData(45, SeverityBand.Severe, "Severe signs expected")]
    [InlineData(65, SeverityBand.Seizures, "Seizures possible")]
    public void Band_text_follows_the_dose(double doseMgPerKg, SeverityBand band, string title)
    {
        // 1 mg/g eaten by a 1 kg dog gives a dose equal to the grams eaten.
        var result = ToxicityCalculator.Calculate(1, doseMgPerKg, 1);

        var viewModel = new ResultViewModel(result, 1, doseMgPerKg, "test chocolate");

        Assert.Equal(band, viewModel.Band);
        Assert.Equal(title, viewModel.BandTitle);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.ExpectedSigns));
    }

    [Fact]
    public void Shows_the_working()
    {
        var result = ToxicityCalculator.Calculate(ChocolateType.Milk, 100, 8);

        var viewModel = new ResultViewModel(result, 8, 100, "milk chocolate");

        Assert.Equal("28.8 mg/kg", viewModel.Dose);
        Assert.Equal("2.3 mg/g x 100 g = 230 mg theobromine and caffeine, divided by 8 kg.", viewModel.Working);
        Assert.Equal(ClinicalText.Disclaimer, viewModel.Disclaimer);
    }
}
