using System.Windows;
using System.Windows.Controls;

namespace WpfApp.Views
{
    public partial class LoginView : Page
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var email = TxtUser.Text;
            var pass = TxtPass.Password; // En prod usar SecureString

            bool success = await MainWindow.ApiClient.LoginAsync(email, pass);
            if (success)
            {
                MessageBox.Show("Login Correcto");
                NavigationService.Navigate(new MasterView());
            }
            else
            {
                MessageBox.Show("Credenciales incorrectas");
            }
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var email = TxtUser.Text;
            var pass = TxtPass.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Introduce un correo y una contraseña para crear la cuenta.");
                return;
            }

            await MainWindow.ApiClient.RegisterAsync(email, pass);
        }
    }
}
