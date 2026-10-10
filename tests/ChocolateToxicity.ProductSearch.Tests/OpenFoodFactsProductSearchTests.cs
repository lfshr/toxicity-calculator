using System.Net;
using System.Text;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ProductSearch.Tests;

public class OpenFoodFactsProductSearchTests
{
    // Shaped like a /cgi/search.pl response with the fields the app asks for.
    private const string SearchResponse = """
        {
          "count": 6, "page": 1, "page_size": 25,
          "products": [
            {
              "code": "3046920022606",
              "product_name": "Excellence 70% Cacao",
              "brands": "Lindt,Lindt & Sprüngli",
              "quantity": "100 g",
              "product_quantity": "100",
              "product_quantity_unit": "g",
              "nutriments": { "cocoa": 70, "cocoa_100g": 70, "energy-kcal_100g": 566 },
              "categories_tags": ["en:snacks", "en:chocolates", "en:dark-chocolates"]
            },
            {
              "code": "1",
              "product_name": "Noir 85%",
              "brands": "Côte d'Or",
              "product_quantity": 100,
              "nutriments": {},
              "categories_tags": ["en:dark-chocolates"]
            },
            {
              "code": "2",
              "product_name": "Dairy Milk 30% less sugar",
              "brands": "Cadbury",
              "quantity": "85 g",
              "product_quantity": 85,
              "categories_tags": ["en:chocolates", "en:milk-chocolates"]
            },
            {
              "code": "3",
              "product_name": "Bournville Cocoa",
              "brands": "Cadbury",
              "nutriments": { "cocoa_100g": 100 },
              "categories_tags": ["en:cocoa-and-its-products", "en:cocoa-powders"]
            },
            {
              "code": "4",
              "product_name": "Milkybar",
              "nutriments": { "cocoa_100g": "28" },
              "categories_tags": ["en:white-chocolates"]
            },
            { "code": "5", "product_name": "" },
            {
              "code": "6",
              "product_name": "Brownie",
              "product_quantity": 2,
              "product_quantity_unit": "kg"
            }
          ]
        }
        """;

    private static IReadOnlyList<ChocolateProduct> Parse() =>
        OpenFoodFactsProductSearch.ParseSearchResponse(System.Text.Json.JsonDocument.Parse(SearchResponse).RootElement);

    [Fact]
    public void Reads_cocoa_from_label_data_first()
    {
        var lindt = Parse()[0];

        Assert.Equal("Excellence 70% Cacao", lindt.Name);
        Assert.Equal("Lindt", lindt.Brand);
        Assert.Equal("100 g", lindt.Quantity);
        Assert.Equal(100, lindt.PackWeightGrams);
        Assert.Equal(70, lindt.CocoaPercent);
        Assert.Equal(CocoaPercentSource.LabelData, lindt.CocoaPercentSource);
        Assert.Equal("Excellence 70% Cacao (Lindt)", lindt.DisplayName);
    }

    [Fact]
    public void Falls_back_to_a_percentage_in_the_name_of_a_dark_chocolate()
    {
        var noir = Parse()[1];

        Assert.Equal(85, noir.CocoaPercent);
        Assert.Equal(CocoaPercentSource.ProductName, noir.CocoaPercentSource);
        Assert.Equal(100, noir.PackWeightGrams);
    }

    [Fact]
    public void Ignores_percentages_in_the_name_that_are_not_cocoa()
    {
        var dairyMilk = Parse()[2];

        Assert.Null(dairyMilk.CocoaPercent);
        Assert.Equal(ChocolateType.Milk, dairyMilk.SuggestedType);
    }

    [Fact]
    public void Cocoa_powder_goes_by_type_not_percentage()
    {
        var cocoa = Parse()[3];

        Assert.Null(cocoa.CocoaPercent);
        Assert.Equal(ChocolateType.CocoaPowder, cocoa.SuggestedType);
    }

    [Fact]
    public void White_chocolate_goes_by_type_not_percentage()
    {
        var white = Parse()[4];

        Assert.Null(white.CocoaPercent);
        Assert.Equal(ChocolateType.White, white.SuggestedType);
    }

    [Fact]
    public void Skips_products_without_a_name_and_ignores_non_gram_packs()
    {
        var products = Parse();

        Assert.Equal(6, products.Count);
        Assert.DoesNotContain(products, p => p.Barcode == "5");
        Assert.Null(products.Single(p => p.Barcode == "6").PackWeightGrams);
    }

    // Shaped like a Search-a-licious response, which lists products under "hits" and can give
    // names by language and brands as a list.
    private const string SearchaliciousResponse = """
        {
          "hits": [
            {
              "code": "7622210449283",
              "product_name": { "en": "Dairy Milk", "main": "Dairy Milk" },
              "brands": ["Cadbury"],
              "quantity": "110 g",
              "product_quantity": 110,
              "categories": ["en:chocolates", "en:milk-chocolates"]
            },
            { "code": "2", "product_name": { "fr": "Chocolat noir 72%" }, "brands": "Côte d'Or" }
          ],
          "count": 2, "page": 1, "page_size": 25, "page_count": 1, "took": 5, "timed_out": false, "is_count_exact": true
        }
        """;

    [Fact]
    public void Reads_search_a_licious_hits()
    {
        var products = OpenFoodFactsProductSearch.ParseSearchResponse(
            System.Text.Json.JsonDocument.Parse(SearchaliciousResponse).RootElement);

        Assert.Equal(2, products.Count);
        Assert.Equal("Dairy Milk", products[0].Name);
        Assert.Equal("Cadbury", products[0].Brand);
        Assert.Equal(110, products[0].PackWeightGrams);
        Assert.Equal(ChocolateType.Milk, products[0].SuggestedType);
        Assert.Equal("Chocolat noir 72%", products[1].Name);
        Assert.Equal(72, products[1].CocoaPercent);
    }

    [Fact]
    public async Task Searches_search_a_licious_with_an_identifying_user_agent()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SearchaliciousResponse);
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler));

        var products = await search.SearchAsync("lindt 70%");

        Assert.Equal(2, products.Count);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("search.openfoodfacts.org", request.RequestUri!.Host);
        Assert.Contains("q=lindt%2070%25", request.RequestUri.Query);
        Assert.Contains("ChocolateToxicityCalculator", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Falls_back_to_the_older_search_when_search_a_licious_fails()
    {
        var handler = new StubHandler(
            (HttpStatusCode.ServiceUnavailable, ""),
            (HttpStatusCode.OK, SearchResponse));
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler));

        var products = await search.SearchAsync("lindt");

        Assert.Equal(6, products.Count);
        Assert.Equal(["search.openfoodfacts.org", "world.openfoodfacts.org"], handler.Requests.Select(r => r.RequestUri!.Host));
        Assert.Contains("search_terms=lindt", handler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task Gets_a_products_full_record()
    {
        const string body = """
            { "status": 1, "code": "3046920022606",
              "product": { "code": "3046920022606", "product_name": "Excellence 70% Cacao", "nutriments": { "cocoa_100g": 70 } } }
            """;
        var handler = new StubHandler(HttpStatusCode.OK, body);

        var product = await new OpenFoodFactsProductSearch(new HttpClient(handler)).GetProductAsync("3046920022606");

        Assert.Equal(70, product?.CocoaPercent);
        Assert.Equal(CocoaPercentSource.LabelData, product?.CocoaPercentSource);
        Assert.Equal("/api/v2/product/3046920022606.json", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Missing_product_record_gives_null()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, """{ "status": 0 }""");

        Assert.Null(await new OpenFoodFactsProductSearch(new HttpClient(handler)).GetProductAsync("123"));
    }

    [Fact]
    public async Task Empty_query_does_not_call_the_api()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SearchResponse);

        var products = await new OpenFoodFactsProductSearch(new HttpClient(handler)).SearchAsync("  ");

        Assert.Empty(products);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "Too many searches")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "returned an error (503)")]
    public async Task Http_errors_become_readable_messages(HttpStatusCode status, string expected)
    {
        var search = new OpenFoodFactsProductSearch(new HttpClient(new StubHandler(status, "")));

        var ex = await Assert.ThrowsAsync<ProductSearchException>(() => search.SearchAsync("lindt"));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public async Task Network_failure_becomes_a_readable_message()
    {
        var search = new OpenFoodFactsProductSearch(new HttpClient(new StubHandler(new HttpRequestException("no network"))));

        var ex = await Assert.ThrowsAsync<ProductSearchException>(() => search.SearchAsync("lindt"));

        Assert.Contains("Couldn't reach Open Food Facts", ex.Message);
    }

    [Fact]
    public async Task Unreadable_response_becomes_a_readable_message()
    {
        var search = new OpenFoodFactsProductSearch(new HttpClient(new StubHandler(HttpStatusCode.OK, "<html>")));

        var ex = await Assert.ThrowsAsync<ProductSearchException>(() => search.SearchAsync("lindt"));

        Assert.Contains("couldn't read", ex.Message);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();
        private readonly Exception? _failure;

        public StubHandler(HttpStatusCode status, string body) => _responses.Enqueue((status, body));

        public StubHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            foreach (var response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        public StubHandler(Exception failure) => _failure = failure;

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_failure is not null)
            {
                throw _failure;
            }

            // The last response repeats, so a single response answers every request.
            var (status, body) = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
