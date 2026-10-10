using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class CalculatorPage : ContentPage
{
	public CalculatorPage(CalculatorViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
