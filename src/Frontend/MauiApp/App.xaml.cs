namespace MauiApp;

public partial class App : Application
{
    public static Services.MauiApiClient ApiClient = new();

	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
