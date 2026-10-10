using System.Globalization;
using ChocolateToxicity.ProductSearch;

namespace ChocolateToxicity.ViewModels;

/// <summary>One row in the product search results.</summary>
public sealed class ProductResult(ChocolateProduct product)
{
    public ChocolateProduct Product { get; } = product;

    public string Title => Product.DisplayName;

    public string? ImageUrl => Product.ImageUrl;

    /// <summary>Pack size and what the app will use for the chocolate, such as "100 g · 70% cocoa".</summary>
    public string Detail
    {
        get
        {
            var parts = new List<string>();
            if (Product.Quantity is { } quantity)
            {
                parts.Add(quantity);
            }

            if (Product.CocoaPercent is { } cocoa)
            {
                parts.Add($"{cocoa.ToString("0.#", CultureInfo.CurrentCulture)}% cocoa");
            }
            else if (Product.SuggestedType is { } type)
            {
                parts.Add(ChocolateTypeOption.For(type).Name);
            }
            else if (parts.Count == 0)
            {
                parts.Add("Tap to check cocoa content");
            }

            return string.Join(" · ", parts);
        }
    }
}
