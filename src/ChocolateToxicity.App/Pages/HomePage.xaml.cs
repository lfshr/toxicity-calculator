using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
