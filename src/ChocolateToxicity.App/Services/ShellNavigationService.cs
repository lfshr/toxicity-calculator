using ChocolateToxicity.App.Pages;
using ChocolateToxicity.Core.Toxins;
using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Services;

public sealed class ShellNavigationService(IServiceProvider services) : INavigationService
{
	public Task ShowCalculatorAsync(ToxinKind kind)
	{
		Page page = kind switch
		{
			ToxinKind.Chocolate => services.GetRequiredService<CalculatorPage>(),
			ToxinKind.Xylitol => services.GetRequiredService<XylitolPage>(),
			ToxinKind.Ibuprofen => new PainkillerPage(new PainkillerViewModel(this, Painkiller.Ibuprofen)),
			ToxinKind.Paracetamol => new PainkillerPage(new PainkillerViewModel(this, Painkiller.Paracetamol)),
			ToxinKind.RatBait => services.GetRequiredService<RatBaitPage>(),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
		return Shell.Current.Navigation.PushAsync(page);
	}

	public Task ShowResultAsync(ResultViewModel result) =>
		Shell.Current.Navigation.PushAsync(new ResultPage(result));

	public Task ShowProductSearchAsync(ProductSearchViewModel search) =>
		Shell.Current.Navigation.PushAsync(new ProductSearchPage(search));

	public Task GoBackAsync() => Shell.Current.Navigation.PopAsync();
}
