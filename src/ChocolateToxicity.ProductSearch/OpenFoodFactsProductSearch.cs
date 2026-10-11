using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ProductSearch;

/// <summary>
/// Searches the Open Food Facts product database (https://world.openfoodfacts.org), a free, open database.
/// </summary>
/// <remarks>
/// Full-text search goes to Search-a-licious (https://search.openfoodfacts.org), the search service Open Food Facts
/// recommends, and falls back to the older /cgi/search.pl endpoint if that fails. The older search is often busy
/// and answers 503 (rate limiting included), so it is tried three times, a few seconds apart; if it still fails,
/// products are looked up by brand through the /api/v2/search endpoint, which is tried twice. Open Food Facts limits search to 10 requests a minute per IP address, so search runs
/// when the user asks rather than as they type. Search results may lack nutriment data, so the chosen product's
/// full record is fetched from the product API (15 requests a minute). Requests must carry a User-Agent naming the
/// app: https://openfoodfacts.github.io/openfoodfacts-server/api/
/// </remarks>
/// <param name="httpClient">The client to send requests with.</param>
/// <param name="useSearchService">
/// False to go straight to the older search. Search-a-licious sends no CORS headers, so a browser blocks it, while
/// world.openfoodfacts.org allows requests from any site. Defaults to false when running in a browser.
/// </param>
/// <param name="retryDelay">
/// How long to wait before trying a busy search again, doubling for a third try. Defaults to two seconds.
/// </param>
public sealed partial class OpenFoodFactsProductSearch(HttpClient httpClient, bool? useSearchService = null, TimeSpan? retryDelay = null)
    : IProductSearch
{
    private readonly bool _useSearchService = useSearchService ?? !OperatingSystem.IsBrowser();
    private readonly TimeSpan _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);

    public const string UserAgent = "ChocolateToxicityCalculator/1.0 (https://github.com/lfshr/toxicity-calculator)";

    private const string SearchUrl = "https://search.openfoodfacts.org/search";
    private const string LegacySearchUrl = "https://world.openfoodfacts.org/cgi/search.pl";
    private const string ProductUrl = "https://world.openfoodfacts.org/api/v2/product/";
    private const string TagSearchUrl = "https://world.openfoodfacts.org/api/v2/search";

    // Limits search to chocolate, cocoa powder and other cocoa products, so "dairy milk" finds the chocolate bar
    // rather than milk.
    private const string CocoaCategory = "en:cocoa-and-its-products";
    // Below this, a percentage in a dark chocolate's name is not its cocoa content.
    private const double DarkChocolateMinimumCocoaPercent = 45;

    private const int LegacySearchAttempts = 3;
    private const int BrandSearchAttempts = 2;
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
        if (_useSearchService)
        {
            try
            {
                using var document = await GetJsonAsync($"{SearchUrl}?q={terms}&langs=en,main&page_size={PageSize}&fields={Fields}", cancellationToken);
                return ParseSearchResponse(document.RootElement);
            }
            catch (ProductSearchException)
            {
                // Fall through to the older search.
            }
        }

        var legacyUrl = $"{LegacySearchUrl}?search_terms={terms}&search_simple=1&action=process&json=1&page_size={PageSize}" +
            $"&tagtype_0=categories&tag_contains_0=contains&tag_0={CocoaCategory}&fields={Fields}";
        try
        {
            using var legacy = await GetJsonWithRetriesAsync(legacyUrl, LegacySearchAttempts, cancellationToken);
            return ParseSearchResponse(legacy.RootElement);
        }
        catch (ProductSearchException ex) when (ex.IsTransient)
        {
            return await SearchByBrandAsync(query, ex, cancellationToken);
        }
    }

    /// <summary>
    /// Looks the query up as a brand ("Lindt 70" as the brand "lindt-70" or "lindt"), listing products whose name or
    /// brand contains every word of the query first. Used when full-text search is unavailable.
    /// </summary>
    private async Task<IReadOnlyList<ChocolateProduct>> SearchByBrandAsync(
        string query, ProductSearchException searchFailure, CancellationToken cancellationToken)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var brands = new[] { ToTag(query), ToTag(words[0]) }.Where(t => t.Length > 0).Distinct();

        JsonDocument document;
        try
        {
            document = await GetJsonWithRetriesAsync(
                $"{TagSearchUrl}?brands_tags={Uri.EscapeDataString(string.Join('|', brands))}&categories_tags={CocoaCategory}" +
                $"&sort_by=unique_scans_n&page_size={PageSize}&fields={Fields}",
                BrandSearchAttempts,
                cancellationToken);
        }
        catch (ProductSearchException)
        {
            throw searchFailure;
        }

        using (document)
        {
            var products = ParseSearchResponse(document.RootElement);
            return products
                .OrderBy(p => words.All(w => $"{p.Name} {p.Brand}".Contains(w, StringComparison.OrdinalIgnoreCase)) ? 0 : 1)
                .ToList();
        }
    }

    /// <summary>Turns text into an Open Food Facts tag: "Hershey's" becomes "hershey-s" and "Côte d'Or" "cote-d-or".</summary>
    public static string ToTag(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().TrimEnd('-');
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

    /// <summary>Gets JSON, trying again after a transient failure such as a busy server.</summary>
    private async Task<JsonDocument> GetJsonWithRetriesAsync(string url, int attempts, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await GetJsonAsync(url, cancellationToken);
            }
            catch (ProductSearchException ex) when (ex.IsTransient && attempt < attempts)
            {
                await Task.Delay(_retryDelay * attempt, cancellationToken);
            }
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Browsers send their own User-Agent and won't let a page replace it; adding one would only trigger
        // a CORS preflight request, so the web app leaves it out.
        if (!OperatingSystem.IsBrowser())
        {
            request.Headers.UserAgent.ParseAdd(UserAgent);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ProductSearchException("Too many searches. Wait a minute and try again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProductSearchException(
                    $"Open Food Facts returned an error ({(int)response.StatusCode}). Try again shortly.",
                    isTransient: (int)response.StatusCode >= 500);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // In a browser this is also how a busy server looks: its error page carries no CORS headers, so the
            // browser hides the response and reports a failed request.
            throw new ProductSearchException(
                "Couldn't reach Open Food Facts. It may be busy, so try again in a minute, and check the internet connection.",
                ex, isTransient: true);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProductSearchException("Open Food Facts took too long to answer. Try again.", ex, isTransient: true);
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
        var suggestedType = SuggestType(name, categories);

        double? cocoa = null;
        CocoaPercentSource? source = null;
        string? caution = null;

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

            // 100% cocoa is either an unsweetened bar (15.5 mg/g) or cocoa powder (28.5 mg/g), and the record
            // doesn't say which, so the vet chooses rather than the app assuming the weaker one.
            if (cocoa >= 100)
            {
                (cocoa, source, suggestedType) = (null, null, null);
                caution = "100% cocoa: choose Unsweetened baking chocolate for a bar, or Cocoa powder for a powder.";
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
            ImageUrl: GetImageUrl(product),
            Caution: caution);
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

    private static ChocolateType? SuggestType(string name, IReadOnlyCollection<string> categories)
    {
        // Not every powder carries the powder category, so a name or category that says cocoa powder counts too.
        if (categories.Contains("en:cocoa-powders") ||
            categories.Any(c => CocoaPowderWords().IsMatch(c)) ||
            CocoaPowderWords().IsMatch(name))
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
    // a cocoa figure, so "30% less sugar" on a milk bar is not read as cocoa. A percentage next to a cocoa word
    // ("70% cocoa", "cacao 85%") wins over the first one, so "Noir 70% aux noisettes 15%" gives 70.
    private static double? GetCocoaFromName(string name, IReadOnlyCollection<string> categories)
    {
        // Where more than one percentage sits next to a cocoa word, the highest errs towards treating.
        double? value = PercentNextToCocoaWord().Matches(name)
            .Select(m => (double?)ParsePercent((m.Groups["before"].Success ? m.Groups["before"] : m.Groups["after"]).Value))
            .Max();
        if (value is null)
        {
            var first = PercentInName().Match(name);
            if (!first.Success || !(CocoaWord().IsMatch(name) || categories.Contains("en:dark-chocolates")))
            {
                return null;
            }

            value = ParsePercent(first.Groups[1].Value);
        }

        if (value is < 20 or > 100)
        {
            return null;
        }

        // Dark chocolate is at least 45% cocoa in practice, so a lower figure in a dark bar's name is something
        // else, such as "with 20% hazelnuts" or "30% less sugar". The dark type (70% assumed) is used instead.
        if (categories.Contains("en:dark-chocolates") && value < DarkChocolateMinimumCocoaPercent)
        {
            return null;
        }

        return value;
    }

    private static double ParsePercent(string text) => double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);

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

    // A percentage straight after a cocoa word ("cacao 85%", "Noir 70%") or straight before one ("70% cocoa",
    // "85 % dark"), with only spaces, colons or "de"/"di" between.
    [GeneratedRegex(
        @"\b(?:cocoa|cacao|cacau|kakao|dark|noir|bitter|fondente|negro|puur)\b(?:\s*(?:solids|:|de|di))?\s*(?<before>\d{1,3}(?:[.,]\d+)?)\s*%" +
        @"|(?<after>\d{1,3}(?:[.,]\d+)?)\s*%\s*(?:de\s+|di\s+)?(?:cocoa|cacao|cacau|kakao|dark|noir|bitter|fondente|negro|puur)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex PercentNextToCocoaWord();

    // "Cocoa powder", "cacao en poudre", "Kakaopulver", "cacaopoeder", "cacau em pó", "en:unsweetened-cocoa-powders".
    [GeneratedRegex(
        @"(?:cocoa|cacao|cacau|kakao)[\s-]*(?:(?:en|em|in)\s+)?(?:powder|poudre|pulver|polvo|poeder|polvere|pó\b)" +
        @"|(?:powder|poudre|pulver|polvo|poeder|polvere)\s+(?:de\s+|di\s+)?(?:cocoa|cacao|cacau|kakao)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CocoaPowderWords();
}
