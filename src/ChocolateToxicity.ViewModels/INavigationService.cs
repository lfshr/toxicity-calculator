namespace ChocolateToxicity.ViewModels;

/// <summary>Moves between screens. Implemented by the app so view models stay free of UI types.</summary>
public interface INavigationService
{
    Task ShowResultAsync(ResultViewModel result);
}
