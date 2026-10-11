using ChocolateToxicity.App.Pages;
using ChocolateToxicity.App.Services;
using ChocolateToxicity.ProductSearch;
using ChocolateToxicity.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChocolateToxicity.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton<IProductSearch>(_ =>
			new OpenFoodFactsProductSearch(new HttpClient { Timeout = TimeSpan.FromSeconds(20) }));
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<CalculatorViewModel>();
		builder.Services.AddTransient<CalculatorPage>();
		builder.Services.AddTransient<XylitolViewModel>();
		builder.Services.AddTransient<XylitolPage>();
		builder.Services.AddTransient<RatBaitViewModel>();
		builder.Services.AddTransient<RatBaitPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
