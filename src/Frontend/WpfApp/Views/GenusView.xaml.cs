using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp.Models;

namespace WpfApp.Views
{
    public partial class GenusView : Page
    {
        public GenusView(GenusGroup group)
        {
            InitializeComponent();
            TxtTitle.Text = $"Género {group.Genus} ({group.Species.Count} especies)";
            SpeciesItems.ItemsSource = group.Species;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private async void SpeciesCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is global::Models.AntSpecies species)
            {
                var desc = await MainWindow.ApiClient.GetDescriptionAsync(species.Id);
                // Navegamos a una vista de detalle bonita dentro de la propia app
                NavigationService?.Navigate(new SpeciesDetailView(species, desc));
            }
        }
    }
}

