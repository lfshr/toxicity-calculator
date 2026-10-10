using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ProductSearch;

/// <summary>
/// Searches the Open Food Facts product database (https://world.openfoodfacts.org), a free, open database.
/// </summary>
/// <remarks>
/// Full-text search uses the /cgi/search.pl endpoint. Open Food Facts limits search to 10 requests a minute
/// per IP address, so search runs when the user asks rather than as they type. Requests must carry a
/// User-Agent naming the app: https://openfoodfacts.github.io/openfoodfacts-server/api/
/// </remarks>
public sealed partial class OpenFoodFactsProductSearch(HttpClient httpClient) : IProductSearch
{
    public const string UserAgent = "ChocolateToxicityCalculator/1.0 (https://github.com/lfshr/toxicity-calculator)";

    private const string BaseUrl = "https://world.openfoodfacts.org/cgi/search.pl";
    private const string Fields = "code,product_name,brands,quantity,product_quantity,product_quantity_unit,nutriments,categories_tags";
    private const int PageSize = 25;

    public async Task<IReadOnlyList<ChocolateProduct>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var url = $"{BaseUrl}?search_terms={Uri.EscapeDataString(query.Trim())}&search_simple=1&action=process&json=1" +
                  $"&page_size={PageSize}&fields={Fields}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ProductSearchException("Too many searches. Wait a minute and try again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProductSearchException($"Open Food Facts returned an error ({(int)response.StatusCode}). Try again shortly.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ParseSearchResponse(document.RootElement);
        }
        catch (HttpRequestException ex)
        {
            throw new ProductSearchException("Couldn't reach Open Food Facts. Check the internet connection.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProductSearchException("Open Food Facts took too long to answer. Try again.", ex);
        }
        catch (JsonException ex)
        {
            throw new ProductSearchException("Open Food Facts sent a response this app couldn't read.", ex);
        }
    }

    /// <summary>Reads the products from a search response, skipping any without a name.</summary>
    public static IReadOnlyList<ChocolateProduct> ParseSearchResponse(JsonElement root)
    {
        if (!root.TryGetProperty("products", out var products) || products.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<ChocolateProduct>();
        foreach (var product in products.EnumerateArray())
        {
            if (ParseProduct(product) is { } parsed)
            {
                results.Add(parsed);
            }
        }

        return results;
    }

    public static ChocolateProduct? ParseProduct(JsonElement product)
    {
        var name = GetString(product, "product_name")?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var brand = GetString(product, "brands")?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var categories = GetCategories(product);
        var suggestedType = SuggestType(categories);

        double? cocoa = null;
        CocoaPercentSource? source = null;

        // Powder and white chocolate always go by type. A percentage understates defatted cocoa powder, and
        // white chocolate's cocoa content is cocoa butter, which carries almost no theobromine.
        if (suggestedType is not (ChocolateType.CocoaPowder or ChocolateType.White))
        {
            if (GetCocoaFromNutriments(product) is { } labelCocoa)
            {
                (cocoa, source) = (labelCocoa, CocoaPercentSource.LabelData);
            }
            else if (GetCocoaFromName(name, categories) is { } nameCocoa)
            {
                (cocoa, source) = (nameCocoa, CocoaPercentSource.ProductName);
            }
        }

        return new ChocolateProduct(
            Barcode: GetString(product, "code") ?? "",
            Name: name,
            Brand: brand,
            Quantity: GetString(product, "quantity")?.Trim() is { Length: > 0 } quantity ? quantity : null,
            PackWeightGrams: GetPackWeightGrams(product),
            CocoaPercent: cocoa,
            CocoaPercentSource: source,
            SuggestedType: suggestedType);
    }

    private static ChocolateType? SuggestType(IReadOnlyCollection<string> categories)
    {
        if (categories.Contains("en:cocoa-powders") || categories.Any(c => c.Contains("unsweetened-cocoa-powder", StringComparison.Ordinal)))
        {
            return ChocolateType.CocoaPowder;
        }

        if (categories.Contains("en:white-chocolates"))
        {
            return ChocolateType.White;
        }

        if (categories.Contains("en:milk-chocolates"))
        {
            return ChocolateType.Milk;
        }

        return null;
    }

    // Open Food Facts records "Cocoa (minimum)" from the label as the "cocoa" nutriment, in %.
    private static double? GetCocoaFromNutriments(JsonElement product)
    {
        if (!product.TryGetProperty("nutriments", out var nutriments) || nutriments.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var key in (string[])["cocoa_100g", "cocoa", "cocoa_value"])
        {
            if (GetNumber(nutriments, key) is > 0 and <= 100 and var value)
            {
                return value;
            }
        }

        return null;
    }

    // Names like "Excellence 70% Cocoa" or "Noir 85%". Only trusted when the name or category says it is
    // a cocoa figure, so "30% less sugar" on a milk bar is not read as cocoa.
    private static double? GetCocoaFromName(string name, IReadOnlyCollection<string> categories)
    {
        var match = PercentInName().Match(name);
        if (!match.Success)
        {
            return null;
        }

        var looksLikeCocoa = CocoaWord().IsMatch(name) || categories.Contains("en:dark-chocolates");
        if (!looksLikeCocoa)
        {
            return null;
        }

        var value = double.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        return value is >= 20 and <= 100 ? value : null;
    }

    private static double? GetPackWeightGrams(JsonElement product)
    {
        var unit = GetString(product, "product_quantity_unit");
        if (unit is not null && !unit.Equals("g", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return GetNumber(product, "product_quantity") is > 0 and <= 5000 and var grams ? grams : null;
    }

    private static IReadOnlyCollection<string> GetCategories(JsonElement product)
    {
        if (!product.TryGetProperty("categories_tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return tags.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    // Open Food Facts sends some numbers as JSON strings, so accept either.
    private static double? GetNumber(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => null,
        };
    }

    [GeneratedRegex(@"(\d{1,3}(?:[.,]\d+)?)\s*%")]
    private static partial Regex PercentInName();

    [GeneratedRegex(@"cocoa|cacao|cacau|kakao|dark|noir|bitter|fondente|negro|puur", RegexOptions.IgnoreCase)]
    private static partial Regex CocoaWord();
}
