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

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Nav_Login(object sender, RoutedEventArgs e) => MainFrame.Navigate(new LoginView());
        private void Nav_Master(object sender, RoutedEventArgs e) => MainFrame.Navigate(new MasterView());
        private void Nav_Mcp(object sender, RoutedEventArgs e) => MainFrame.Navigate(new McpView());
        private void Logo_Click(object sender, MouseButtonEventArgs e) => MainFrame.Navigate(new MasterView());
    }
}