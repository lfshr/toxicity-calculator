using ChocolateToxicity.Core;
using ChocolateToxicity.ProductSearch;

namespace ChocolateToxicity.ViewModels.Tests;

public class ProductSearchViewModelTests
{
    private readonly FakeProductSearch _search = new();
    private readonly FakeNavigation _navigation = new();
    private readonly List<ChocolateProduct> _chosen = [];
    private readonly ProductSearchViewModel _viewModel;

    public ProductSearchViewModelTests()
    {
        _viewModel = new ProductSearchViewModel(_search, _navigation, _chosen.Add);
    }

    [Fact]
    public void Cannot_search_with_an_empty_query()
    {
        Assert.False(_viewModel.SearchCommand.CanExecute(null));

        _viewModel.Query = "lindt";

        Assert.True(_viewModel.SearchCommand.CanExecute(null));
    }

    [Fact]
    public async Task Shows_results()
    {
        _search.Products =
        [
            new("1", "Excellence 70%", "Lindt", "100 g", 100, 70, CocoaPercentSource.ProductName, null),
            new("2", "Dairy Milk", "Cadbury", "110 g", 110, null, null, ChocolateType.Milk),
            new("3", "Mystery bar", null, null, null, null, null, null),
        ];
        _viewModel.Query = "chocolate";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("chocolate", _search.LastQuery);
        Assert.Null(_viewModel.Message);
        Assert.False(_viewModel.IsSearching);
        Assert.Equal(["100 g · 70% cocoa", "110 g · Milk chocolate", "Tap to check cocoa content"],
            _viewModel.Results.Select(r => r.Detail));
        Assert.Equal("Excellence 70% (Lindt)", _viewModel.Results[0].Title);
    }

    [Fact]
    public async Task Says_when_nothing_is_found()
    {
        _viewModel.Query = "zzz";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Results);
        Assert.StartsWith("No products with known cocoa content found", _viewModel.Message);
    }

    [Fact]
    public async Task Shows_search_failures()
    {
        _search.Failure = new ProductSearchException("Couldn't reach Open Food Facts.");
        _viewModel.Query = "lindt";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.StartsWith("Couldn't reach Open Food Facts.", _viewModel.Message);
        Assert.False(_viewModel.IsSearching);
    }

    [Fact]
    public async Task Choosing_a_product_uses_its_full_record_when_that_has_cocoa_data()
    {
        var found = new ChocolateProduct("1", "Bar", null, null, null, null, null, null);
        var details = found with { CocoaPercent = 70, CocoaPercentSource = CocoaPercentSource.LabelData };
        _search.Details["1"] = details;

        await _viewModel.ChooseCommand.ExecuteAsync(new ProductResult(found));

        Assert.Equal([details], _chosen);
        Assert.False(_viewModel.IsSearching);
        Assert.Null(_viewModel.Message);
    }

    [Fact]
    public async Task Choosing_a_product_hands_it_back_and_closes()
    {
        var product = new ChocolateProduct("1", "Bar", null, null, null, 50, CocoaPercentSource.LabelData, null);

        await _viewModel.ChooseCommand.ExecuteAsync(new ProductResult(product));

        Assert.Equal([product], _chosen);
        Assert.Equal(1, _navigation.BackCount);
    }
}
