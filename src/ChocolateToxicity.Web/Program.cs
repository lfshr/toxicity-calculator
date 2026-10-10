using ChocolateToxicity.Core.Toxins;
using ChocolateToxicity.ProductSearch;
using ChocolateToxicity.ViewModels;
using ChocolateToxicity.Web;
using ChocolateToxicity.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
builder.Services.AddScoped<IProductSearch, OpenFoodFactsProductSearch>();
builder.Services.AddScoped<PatientState>();
builder.Services.AddScoped<WebNavigationService>();
builder.Services.AddScoped<INavigationService>(sp => sp.GetRequiredService<WebNavigationService>());

// One instance of each form for the app's lifetime, so going to a result and back keeps what was typed.
builder.Services.AddScoped<CalculatorViewModel>();
builder.Services.AddScoped<GrapesViewModel>();
builder.Services.AddScoped<XylitolViewModel>();
builder.Services.AddScoped<RatBaitViewModel>();
builder.Services.AddKeyedScoped(Painkiller.Ibuprofen, (sp, _) => new PainkillerViewModel(sp.GetRequiredService<INavigationService>(), Painkiller.Ibuprofen));
builder.Services.AddKeyedScoped(Painkiller.Paracetamol, (sp, _) => new PainkillerViewModel(sp.GetRequiredService<INavigationService>(), Painkiller.Paracetamol));

await builder.Build().RunAsync();
