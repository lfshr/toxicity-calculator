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
              "image_front_small_url": "http://images.openfoodfacts.org/images/products/304/692/002/2606/front_en.200.jpg",
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
        Assert.Equal("https://images.openfoodfacts.org/images/products/304/692/002/2606/front_en.200.jpg", lindt.ImageUrl);
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
    public void Dark_chocolate_without_a_percentage_is_treated_as_strong_dark()
    {
        var json = """{ "code": "9", "product_name": "Plain chocolate", "categories_tags": ["en:dark-chocolates"] }""";

        var product = OpenFoodFactsProductSearch.ParseProduct(System.Text.Json.JsonDocument.Parse(json).RootElement);

        Assert.Null(product?.CocoaPercent);
        Assert.Equal(ChocolateType.StrongDark, product?.SuggestedType);
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

    private static ChocolateProduct ParseOne(string json) =>
        OpenFoodFactsProductSearch.ParseProduct(System.Text.Json.JsonDocument.Parse(json).RootElement)!;

    [Theory]
    [InlineData("Dark chocolate with 20% hazelnuts")]
    [InlineData("30% Less Sugar Dark Chocolate")]
    [InlineData("Dark Chocolate 35% Orange")]
    public void A_low_percentage_in_a_dark_chocolates_name_falls_back_to_the_dark_type(string name)
    {
        var product = ParseOne($$"""{ "code": "9", "product_name": "{{name}}", "categories_tags": ["en:dark-chocolates"] }""");

        Assert.Null(product.CocoaPercent);
        Assert.Equal(ChocolateType.StrongDark, product.SuggestedType);
    }

    [Theory]
    [InlineData("Noir 70% aux noisettes 15%", 70)]
    [InlineData("Hazelnut 15% dark 72% cocoa", 72)]
    [InlineData("Excellence 85% Cocoa", 85)]
    [InlineData("Cacao 90%", 90)]
    [InlineData("Lindt Excellence 78%", 78)]
    public void Takes_the_percentage_next_to_a_cocoa_word(string name, double cocoa)
    {
        var product = ParseOne($$"""{ "code": "9", "product_name": "{{name}}", "categories_tags": ["en:dark-chocolates"] }""");

        Assert.Equal(cocoa, product.CocoaPercent);
        Assert.Equal(CocoaPercentSource.ProductName, product.CocoaPercentSource);
    }

    [Theory]
    [InlineData("Organic raw cacao powder 100%")]
    [InlineData("Cacao en poudre non sucré")]
    [InlineData("Kakaopulver stark entölt")]
    [InlineData("Cocoa Powder")]
    public void Cocoa_powder_is_recognised_from_its_name(string name)
    {
        var product = ParseOne($$"""
            { "code": "9", "product_name": "{{name}}", "nutriments": { "cocoa_100g": 100 },
              "categories_tags": ["en:cocoa-and-its-products"] }
            """);

        Assert.Null(product.CocoaPercent);
        Assert.Equal(ChocolateType.CocoaPowder, product.SuggestedType);
        Assert.Equal(MethylxanthineContent.CocoaPowder, MethylxanthineContent.ForType(product.SuggestedType!.Value));
    }

    [Fact]
    public void Cocoa_powder_is_recognised_from_a_powder_category()
    {
        var product = ParseOne("""
            { "code": "9", "product_name": "Bio Kakao", "nutriments": { "cocoa_100g": 100 },
              "categories_tags": ["en:cocoa-and-its-products", "de:kakaopulver"] }
            """);

        Assert.Equal(ChocolateType.CocoaPowder, product.SuggestedType);
    }

    [Fact]
    public void Hundred_percent_cocoa_that_may_be_powder_asks_for_the_type()
    {
        var product = ParseOne("""
            { "code": "9", "product_name": "Pure cacao", "nutriments": { "cocoa_100g": 100 },
              "categories_tags": ["en:cocoa-and-its-products"] }
            """);

        Assert.Null(product.CocoaPercent);
        Assert.Null(product.SuggestedType);
        Assert.Contains("Cocoa powder", product.Caution);
    }

    [Fact]
    public void Cacao_pods_and_nibs_are_not_powder()
    {
        var product = ParseOne("""{ "code": "9", "product_name": "Cacao nibs 100%", "categories_tags": ["en:cocoa-and-its-products"] }""");

        Assert.NotEqual(ChocolateType.CocoaPowder, product.SuggestedType);
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
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler), retryDelay: TimeSpan.Zero);

        var products = await search.SearchAsync("lindt");

        // The brownie has no known cocoa content, so it is left out.
        Assert.Equal(5, products.Count);
        Assert.All(products, p => Assert.True(p.HasCocoaInfo));
        Assert.Equal(["search.openfoodfacts.org", "world.openfoodfacts.org"], handler.Requests.Select(r => r.RequestUri!.Host));
        Assert.Contains("search_terms=lindt", handler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task Can_skip_search_a_licious_for_browsers()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SearchResponse);
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler), useSearchService: false);

        var products = await search.SearchAsync("lindt");

        Assert.Equal(5, products.Count);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("world.openfoodfacts.org", request.RequestUri!.Host);
        Assert.Contains("search_terms=lindt", request.RequestUri.Query);
        // Only cocoa products, so "dairy milk" finds the chocolate rather than milk.
        Assert.Contains("tag_0=en:cocoa-and-its-products", request.RequestUri.Query);
    }

    [Fact]
    public async Task Tries_the_older_search_again_when_it_is_busy()
    {
        var handler = new StubHandler(
            (HttpStatusCode.ServiceUnavailable, "<html>"),
            (HttpStatusCode.ServiceUnavailable, "<html>"),
            (HttpStatusCode.OK, SearchResponse));
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler), useSearchService: false, retryDelay: TimeSpan.Zero);

        var products = await search.SearchAsync("lindt");

        Assert.Equal(5, products.Count);
        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, r => Assert.Equal("/cgi/search.pl", r.RequestUri!.AbsolutePath));
    }

    [Fact]
    public async Task Looks_the_query_up_as_a_brand_when_the_older_search_stays_busy()
    {
        const string brandResponse = """
            { "count": 2, "products": [
              { "code": "1", "product_name": "Buttons", "brands": "Cadbury, Dairy", "categories_tags": ["en:milk-chocolates"] },
              { "code": "2", "product_name": "Dairy Milk", "brands": "Cadbury", "categories_tags": ["en:milk-chocolates"] } ] }
            """;
        var handler = new StubHandler(
            (HttpStatusCode.ServiceUnavailable, "<html>"),
            (HttpStatusCode.ServiceUnavailable, "<html>"),
            (HttpStatusCode.ServiceUnavailable, "<html>"),
            (HttpStatusCode.OK, brandResponse));
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler), useSearchService: false, retryDelay: TimeSpan.Zero);

        var products = await search.SearchAsync("Dairy Milk");

        var request = handler.Requests[3].RequestUri!;
        Assert.Equal("/api/v2/search", request.AbsolutePath);
        Assert.Contains("brands_tags=dairy-milk%7Cdairy", request.Query);
        Assert.Contains("categories_tags=en:cocoa-and-its-products", request.Query);
        // The product named after every word of the query comes first.
        Assert.Equal(["Dairy Milk", "Buttons"], products.Select(p => p.Name));
    }

    [Fact]
    public async Task Reports_the_search_failure_when_brand_lookup_fails_too()
    {
        var handler = new StubHandler(new HttpRequestException("blocked"));
        var search = new OpenFoodFactsProductSearch(new HttpClient(handler), useSearchService: false, retryDelay: TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<ProductSearchException>(() => search.SearchAsync("lindt"));

        Assert.Contains("Couldn't reach Open Food Facts", ex.Message);
        // Three tries of the full-text search, then two of the brand lookup.
        Assert.Equal(5, handler.Requests.Count);
    }

    [Theory]
    [InlineData("Lindt", "lindt")]
    [InlineData("Dairy Milk", "dairy-milk")]
    [InlineData("Hershey's", "hershey-s")]
    [InlineData("Côte d'Or", "cote-d-or")]
    [InlineData("Lindt 70%", "lindt-70")]
    public void Makes_open_food_facts_tags(string text, string tag) => Assert.Equal(tag, OpenFoodFactsProductSearch.ToTag(text));

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
        var search = new OpenFoodFactsProductSearch(new HttpClient(new StubHandler(status, "")), retryDelay: TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<ProductSearchException>(() => search.SearchAsync("lindt"));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public async Task Network_failure_becomes_a_readable_message()
    {
        var search = new OpenFoodFactsProductSearch(new HttpClient(new StubHandler(new HttpRequestException("no network"))), retryDelay: TimeSpan.Zero);

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
