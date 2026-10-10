using ChocolateToxicity.ProductSearch;

namespace ChocolateToxicity.ViewModels.Tests;

internal sealed class FakeNavigation : INavigationService
{
    public ResultViewModel? Shown { get; private set; }

    public ProductSearchViewModel? Search { get; private set; }

    public int BackCount { get; private set; }

    public Task ShowResultAsync(ResultViewModel result)
    {
        Shown = result;
        return Task.CompletedTask;
    }

    public Task ShowProductSearchAsync(ProductSearchViewModel search)
    {
        Search = search;
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        BackCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeProductSearch : IProductSearch
{
    public IReadOnlyList<ChocolateProduct> Products { get; set; } = [];

    public Exception? Failure { get; set; }

    public string? LastQuery { get; private set; }

    public Dictionary<string, ChocolateProduct> Details { get; } = [];

    public Task<IReadOnlyList<ChocolateProduct>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        LastQuery = query;
        return Failure is null ? Task.FromResult(Products) : Task.FromException<IReadOnlyList<ChocolateProduct>>(Failure);
    }

    public Task<ChocolateProduct?> GetProductAsync(string barcode, CancellationToken cancellationToken = default) =>
        Task.FromResult(Details.GetValueOrDefault(barcode));
}
