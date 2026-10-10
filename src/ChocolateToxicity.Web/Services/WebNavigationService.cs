using ChocolateToxicity.ViewModels;
using Microsoft.AspNetCore.Components;

namespace ChocolateToxicity.Web.Services;

/// <summary>
/// Moves between pages for the shared view models. The result and the open product search are kept here,
/// since a URL can't carry them.
/// </summary>
public sealed class WebNavigationService(NavigationManager navigation) : INavigationService
{
    public ResultViewModel? CurrentResult { get; private set; }

    /// <summary>The page the current result came from, for its Back button.</summary>
    public string ResultReturnPath { get; private set; } = "";

    public ProductSearchViewModel? CurrentSearch { get; private set; }

    public static string PathFor(ToxinKind kind) => kind switch
    {
        ToxinKind.Chocolate => "toxins/chocolate",
        ToxinKind.GrapesAndRaisins => "toxins/grapes",
        ToxinKind.Xylitol => "toxins/xylitol",
        ToxinKind.Ibuprofen => "toxins/ibuprofen",
        ToxinKind.Paracetamol => "toxins/paracetamol",
        ToxinKind.RatBait => "toxins/rat-bait",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public Task ShowCalculatorAsync(ToxinKind kind)
    {
        navigation.NavigateTo(PathFor(kind));
        return Task.CompletedTask;
    }

    public Task ShowResultAsync(ResultViewModel result)
    {
        CurrentResult = result;
        ResultReturnPath = navigation.ToBaseRelativePath(navigation.Uri);
        navigation.NavigateTo("result");
        return Task.CompletedTask;
    }

    public Task ShowProductSearchAsync(ProductSearchViewModel search)
    {
        CurrentSearch = search;
        navigation.NavigateTo("toxins/chocolate/search");
        return Task.CompletedTask;
    }

    // Only the product search goes back through the view model, to the chocolate form it was opened from.
    public Task GoBackAsync()
    {
        navigation.NavigateTo(PathFor(ToxinKind.Chocolate));
        return Task.CompletedTask;
    }
}
