using MauiApp.Services;

namespace MauiApp.Views;

public partial class LoginPage : ContentPage
{
    // Servicio global simple (idealmente usar DI)
    public static MauiApiClient ApiClient = new MauiApiClient();

	public LoginPage()
	{
		InitializeComponent();
	}

    private async void BtnLogin_Clicked(object sender, EventArgs e)
    {
        bool success = await ApiClient.LoginAsync(EntryUser.Text, EntryPass.Text);
        if (success)
        {
            await DisplayAlert("Éxito", "Login correcto", "OK");
            // Navegar a la App principal (Shell maneja la navegación)
            await Shell.Current.GoToAsync("//MasterPage"); 
        }
        else
        {
            await DisplayAlert("Error", "Credenciales incorrectas", "OK");
        }
    }
}
