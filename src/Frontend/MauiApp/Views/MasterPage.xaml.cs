using Models;

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
        var data = await App.ApiClient.GetAllAsync();
        CollView.ItemsSource = data;
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SpeciesEditorPage(null));
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is AntSpecies selected)
        {
            await Navigation.PushAsync(new SpeciesEditorPage(selected));
        }
    }

    private async void OnDeleteInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is AntSpecies itemToDelete)
        {
            bool confirm = await DisplayAlert("Borrar", $"¿Eliminar {itemToDelete.ScientificName}?", "Sí", "No");
            if (confirm)
            {
                await App.ApiClient.DeleteAsync(itemToDelete.Id);
                await LoadData(); // Recargar lista
            }
        }
    }
}