using ChocolateToxicity.Core;

namespace ChocolateToxicity.ProductSearch;

/// <summary>Where a product's cocoa percentage came from.</summary>
public enum CocoaPercentSource
{
    /// <summary>The cocoa content recorded from the label's nutrition or ingredients panel.</summary>
    LabelData,

    /// <summary>A percentage in the product name, such as "Excellence 70% Cocoa".</summary>
    ProductName,
}

/// <summary>A product found by search, with whatever the database knows about its cocoa content.</summary>
/// <param name="Barcode">The product's barcode.</param>
/// <param name="Name">Product name as recorded in the database.</param>
/// <param name="Brand">First listed brand, if any.</param>
/// <param name="Quantity">Pack size as printed, such as "100 g".</param>
/// <param name="PackWeightGrams">Pack weight in grams, when known.</param>
/// <param name="CocoaPercent">Cocoa percentage, when known.</param>
/// <param name="CocoaPercentSource">Where <paramref name="CocoaPercent"/> came from.</param>
/// <param name="SuggestedType">
/// A chocolate type implied by the product's category, used when there is no cocoa percentage
/// (or, for cocoa powder and white chocolate, instead of one, since a percentage misstates their content).
/// </param>
/// <param name="ImageUrl">A small photo of the product's front, when there is one.</param>
/// <param name="Caution">Why the vet must choose the type themselves, when the record is ambiguous.</param>
public sealed record ChocolateProduct(
    string Barcode,
    string Name,
    string? Brand,
    string? Quantity,
    double? PackWeightGrams,
    double? CocoaPercent,
    CocoaPercentSource? CocoaPercentSource,
    ChocolateType? SuggestedType,
    string? ImageUrl = null,
    string? Caution = null)
{
    /// <summary>True when the product's cocoa content is known, as a percentage or a chocolate type.</summary>
    public bool HasCocoaInfo => CocoaPercent is not null || SuggestedType is not null;

    /// <summary>Name with the brand, for lists: "Excellence 70% (Lindt)".</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Brand) || Name.Contains(Brand, StringComparison.OrdinalIgnoreCase)
        ? Name
        : $"{Name} ({Brand})";
}
