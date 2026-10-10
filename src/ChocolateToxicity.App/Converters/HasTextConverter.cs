using System.Globalization;

namespace ChocolateToxicity.App.Converters;

/// <summary>True when a string is not null or empty; used to show validation messages only when there is one.</summary>
public sealed class HasTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrEmpty(value as string);

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
