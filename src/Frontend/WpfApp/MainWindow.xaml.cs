using System.Windows;
using System.Windows.Input;
using WpfApp.Services;
using WpfApp.Views;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        // Instancia global del cliente API para compartir el token
        public static WpfApiClient ApiClient = new WpfApiClient();

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginView());
        }

        public void OnLoginSuccess()
        {
            BtnExplora.Visibility = Visibility.Visible;
            BtnComunidad.Visibility = Visibility.Visible;
            BtnAdmin.Visibility = Visibility.Visible;
            BtnSalir.Visibility = Visibility.Visible;
            BtnIngresar.Visibility = Visibility.Collapsed;
            
            // Navegar a la vista principal
            MainFrame.Navigate(new MasterView());
        }

        private void Logout()
        {
            ApiClient.Token = null; // Borrar token si hubiera
            BtnExplora.Visibility = Visibility.Collapsed;
            BtnComunidad.Visibility = Visibility.Collapsed;
            BtnAdmin.Visibility = Visibility.Collapsed;
            BtnSalir.Visibility = Visibility.Collapsed;
            BtnIngresar.Visibility = Visibility.Visible;
            
            MainFrame.Navigate(new LoginView());
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Logout();
        private void Nav_Login(object sender, RoutedEventArgs e) => MainFrame.Navigate(new LoginView());
        private void Nav_Master(object sender, RoutedEventArgs e) => MainFrame.Navigate(new MasterView());
        private void Nav_Mcp(object sender, RoutedEventArgs e) => MainFrame.Navigate(new McpView());
        private void Nav_Admin(object sender, RoutedEventArgs e) => MainFrame.Navigate(new AdminView());
        private void Logo_Click(object sender, MouseButtonEventArgs e)
        {
             if (BtnExplora.Visibility == Visibility.Visible)
                MainFrame.Navigate(new MasterView());
        }
    }
}