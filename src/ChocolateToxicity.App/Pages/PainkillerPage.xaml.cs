using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class PainkillerPage : ContentPage
{
	public PainkillerPage(PainkillerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
