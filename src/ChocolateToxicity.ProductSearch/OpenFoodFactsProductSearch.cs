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
/// Full-text search goes to Search-a-licious (https://search.openfoodfacts.org), the search service Open Food Facts
/// recommends, and falls back to the older /cgi/search.pl endpoint if that fails. Open Food Facts limits search to
/// 10 requests a minute per IP address, so search runs when the user asks rather than as they type. Search results
/// may lack nutriment data, so the chosen product's full record is fetched from the product API (15 requests a
/// minute). Requests must carry a User-Agent naming the app: https://openfoodfacts.github.io/openfoodfacts-server/api/
/// </remarks>
public sealed partial class OpenFoodFactsProductSearch(HttpClient httpClient) : IProductSearch
{
    public const string UserAgent = "ChocolateToxicityCalculator/1.0 (https://github.com/lfshr/toxicity-calculator)";

    private const string SearchUrl = "https://search.openfoodfacts.org/search";
    private const string LegacySearchUrl = "https://world.openfoodfacts.org/cgi/search.pl";
    private const string ProductUrl = "https://world.openfoodfacts.org/api/v2/product/";
    private const string Fields =
        "code,product_name,brands,quantity,product_quantity,product_quantity_unit,nutriments,categories_tags," +
        "image_front_small_url,image_small_url";

    // Results without known cocoa content are dropped, so ask for more than a screenful.
    private const int PageSize = 50;

    /// <summary>Finds products, keeping only those whose cocoa content is known.</summary>
    public async Task<IReadOnlyList<ChocolateProduct>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var products = await SearchAllAsync(query, cancellationToken);
        return products.Where(p => p.HasCocoaInfo).ToList();
    }

    private async Task<IReadOnlyList<ChocolateProduct>> SearchAllAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var terms = Uri.EscapeDataString(query.Trim());
        try
        {
            using var document = await GetJsonAsync($"{SearchUrl}?q={terms}&langs=en,main&page_size={PageSize}&fields={Fields}", cancellationToken);
            return ParseSearchResponse(document.RootElement);
        }
        catch (ProductSearchException)
        {
            using var document = await GetJsonAsync(
                $"{LegacySearchUrl}?search_terms={terms}&search_simple=1&action=process&json=1&page_size={PageSize}&fields={Fields}",
                cancellationToken);
            return ParseSearchResponse(document.RootElement);
        }
    }

    public async Task<ChocolateProduct?> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        try
        {
            using var document = await GetJsonAsync($"{ProductUrl}{Uri.EscapeDataString(barcode)}.json?fields={Fields}", cancellationToken);
            return document.RootElement.TryGetProperty("product", out var product) && product.ValueKind == JsonValueKind.Object
                ? ParseProduct(product)
                : null;
        }
        catch (ProductSearchException)
        {
            return null;
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
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
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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

    /// <summary>
    /// Reads the products from a search response, skipping any without a name. Search-a-licious lists them under
    /// "hits" and the older search under "products".
    /// </summary>
    public static IReadOnlyList<ChocolateProduct> ParseSearchResponse(JsonElement root)
    {
        if (!(root.TryGetProperty("hits", out var products) || root.TryGetProperty("products", out products)) ||
            products.ValueKind != JsonValueKind.Array)
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
        var name = GetText(product, "product_name")?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var brand = GetText(product, "brands")?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
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
            Barcode: GetText(product, "code") ?? "",
            Name: name,
            Brand: brand,
            Quantity: GetString(product, "quantity")?.Trim() is { Length: > 0 } quantity ? quantity : null,
            PackWeightGrams: GetPackWeightGrams(product),
            CocoaPercent: cocoa,
            CocoaPercentSource: source,
            SuggestedType: suggestedType,
            ImageUrl: GetImageUrl(product));
    }

    private static string? GetImageUrl(JsonElement product)
    {
        foreach (var field in (string[])["image_front_small_url", "image_small_url"])
        {
            if (GetString(product, field) is { } url &&
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri.ToString();
            }
        }

        return null;
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

        // Dark bars range from about 45% upwards; strong dark (worked out at 70%) errs towards treating.
        if (categories.Contains("en:dark-chocolates"))
        {
            return ChocolateType.StrongDark;
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
        if (!(product.TryGetProperty("categories_tags", out var tags) || product.TryGetProperty("categories", out tags)) ||
            tags.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return tags.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    // Text that may come as a plain string, a list (Search-a-licious brands), or names by language
    // (Search-a-licious product names), in which case English, then the product's main language, is preferred.
    private static string? GetText(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString();
            case JsonValueKind.Number:
                return value.GetRawText();
            case JsonValueKind.Array:
                var items = value.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String).Select(i => i.GetString()).ToList();
                return items.Count == 0 ? null : string.Join(",", items);
            case JsonValueKind.Object:
                foreach (var language in (string[])["en", "main"])
                {
                    if (value.TryGetProperty(language, out var named) && named.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(named.GetString()))
                    {
                        return named.GetString();
                    }
                }

                return value.EnumerateObject()
                    .Select(p => p.Value)
                    .Where(v => v.ValueKind == JsonValueKind.String)
                    .Select(v => v.GetString())
                    .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
            default:
                return null;
        }
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
