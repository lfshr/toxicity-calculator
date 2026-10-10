using System.Globalization;
using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.App.Converters;

/// <summary>Background colour for the verdict banner. All four take white text.</summary>
public sealed class RiskColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		RiskLevel.Moderate => Color.FromArgb("#9A5B00"),
		RiskLevel.High => Color.FromArgb("#C43E00"),
		RiskLevel.Critical => Color.FromArgb("#B71C1C"),
		_ => Color.FromArgb("#2E7D32"),
	};

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
