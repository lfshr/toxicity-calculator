using System.Collections.ObjectModel;
using ChocolateToxicity.ProductSearch;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateToxicity.ViewModels;

/// <summary>The product search screen. Choosing a product hands it back to the calculator and closes the screen.</summary>
public partial class ProductSearchViewModel(
    IProductSearch productSearch,
    INavigationService navigation,
    Action<ChocolateProduct> onProductChosen) : ObservableObject
{
    public ObservableCollection<ProductResult> Results { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    public partial string Query { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    /// <summary>Shown instead of results: no matches, or why the search failed.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(Query);

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        IsSearching = true;
        Message = null;
        Results.Clear();
        try
        {
            var products = await productSearch.SearchAsync(Query, cancellationToken);
            foreach (var product in products)
            {
                Results.Add(new ProductResult(product));
            }

            if (Results.Count == 0)
            {
                Message = "No products with known cocoa content found. Try the brand and product name, or go back and pick the chocolate type.";
            }
        }
        catch (ProductSearchException ex)
        {
            Message = $"{ex.Message} You can go back and pick the chocolate type instead.";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task ChooseAsync(ProductResult? result)
    {
        if (result is null)
        {
            return;
        }

        // Search results can lack cocoa data, so fetch the full record; fall back to the result if that fails.
        IsSearching = true;
        Message = "Checking the product's cocoa content...";
        try
        {
            var details = await productSearch.GetProductAsync(result.Product.Barcode);
            onProductChosen(details is { CocoaPercent: not null } or { SuggestedType: not null } ? details : result.Product);
        }
        finally
        {
            IsSearching = false;
            Message = null;
        }

        await navigation.GoBackAsync();
    }
}
