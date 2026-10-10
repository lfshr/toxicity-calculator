using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class ResultPage : ContentPage
{
	public ResultPage(ResultViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();
}
