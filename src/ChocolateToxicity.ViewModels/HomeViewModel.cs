using CommunityToolkit.Mvvm.Input;

namespace ChocolateToxicity.ViewModels;

/// <summary>The start screen: choose what the dog has eaten.</summary>
public partial class HomeViewModel(INavigationService navigation)
{
    public IReadOnlyList<ToxinOption> Toxins => ToxinOption.All;

    [RelayCommand]
    private Task ChooseAsync(ToxinOption? option) =>
        option is null ? Task.CompletedTask : navigation.ShowCalculatorAsync(option.Kind);
}
