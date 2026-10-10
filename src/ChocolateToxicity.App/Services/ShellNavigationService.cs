using ChocolateToxicity.App.Pages;
using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Services;

public sealed class ShellNavigationService : INavigationService
{
	public Task ShowResultAsync(ResultViewModel result) =>
		Shell.Current.Navigation.PushAsync(new ResultPage(result));

	public Task ShowProductSearchAsync(ProductSearchViewModel search) =>
		Shell.Current.Navigation.PushAsync(new ProductSearchPage(search));

	public Task GoBackAsync() => Shell.Current.Navigation.PopAsync();
}
