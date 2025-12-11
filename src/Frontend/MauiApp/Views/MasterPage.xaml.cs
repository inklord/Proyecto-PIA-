namespace MauiApp.Views;

public partial class MasterPage : ContentPage
{
	public MasterPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async void BtnLoad_Clicked(object sender, EventArgs e)
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        // Usamos el cliente estático del Login (simplificación)
        var data = await LoginPage.ApiClient.GetAllAsync();
        CollView.ItemsSource = data;
    }
}
