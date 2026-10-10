using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class GrapesPage : ContentPage
{
	public GrapesPage(GrapesViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
