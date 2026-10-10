using ChocolateToxicity.App.Pages;
using ChocolateToxicity.App.Services;
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
		builder.Services.AddTransient<CalculatorViewModel>();
		builder.Services.AddTransient<CalculatorPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
