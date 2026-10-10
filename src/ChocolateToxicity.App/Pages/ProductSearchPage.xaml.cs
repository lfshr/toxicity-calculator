using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.App.Pages;

public partial class ProductSearchPage : ContentPage
{
	public ProductSearchPage(ProductSearchViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
