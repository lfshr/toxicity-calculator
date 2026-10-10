using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class ProductSearchPage : ContentPage
{
	public ProductSearchPage(ProductSearchViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SearchBox.Focus();
	}
}
