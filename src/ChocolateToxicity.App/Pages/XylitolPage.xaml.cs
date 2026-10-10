using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class XylitolPage : ContentPage
{
	public XylitolPage(XylitolViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
