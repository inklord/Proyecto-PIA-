namespace MauiApp;

public static class MauiProgram
{
    // Usamos el nombre completo para evitar conflicto con el namespace 'MauiApp'
	public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
	{
		var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>();

		return builder.Build();
	}
}
