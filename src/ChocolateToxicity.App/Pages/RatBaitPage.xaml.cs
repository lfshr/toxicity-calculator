using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class RatBaitPage : ContentPage
{
	public RatBaitPage(RatBaitViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
