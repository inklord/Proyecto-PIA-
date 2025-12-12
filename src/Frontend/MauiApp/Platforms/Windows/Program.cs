using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace MauiApp;

// Entry point para Windows (necesario para evitar CS5001 cuando el proyecto no incluye el scaffold completo)
public partial class Program : MauiWinUIApplication
{
    protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args)
    {
        MauiWinUIApplication.Start(_ => new Program());
    }
}


