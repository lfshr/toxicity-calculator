namespace ChocolateToxicity.ViewModels;

/// <summary>Moves between screens. Implemented by the app so view models stay free of UI types.</summary>
public interface INavigationService
{
    Task ShowCalculatorAsync(ToxinKind kind);

    Task ShowResultAsync(ResultViewModel result);

    Task ShowProductSearchAsync(ProductSearchViewModel search);

    Task GoBackAsync();
}
