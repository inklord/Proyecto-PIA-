using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp.Models;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WpfApp.Views
{
    public partial class GenusView : Page
    {
        private string _genusName;

        public GenusView(GenusGroup group)
        {
            InitializeComponent();
            _genusName = group.Genus;
            // Carga inicial rápida
            LoadData(group.Species);
            
            // Recargar al mostrar (por si volvemos de editar)
            Loaded += async (s, e) => await RefreshDataAsync();
        }

        private void LoadData(List<global::Models.AntSpecies> species)
        {
            TxtTitle.Text = $"Género {_genusName} ({species.Count} especies)";
            SpeciesItems.ItemsSource = species;
        }

        private async Task RefreshDataAsync()
        {
            var all = await MainWindow.ApiClient.GetAllAsync();
            var filtered = all.Where(s => !string.IsNullOrWhiteSpace(s.ScientificName) 
                                       && s.ScientificName.StartsWith(_genusName))
                              .ToList();
            LoadData(filtered);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is global::Models.AntSpecies species)
            {
                NavigationService?.Navigate(new SpeciesEditorView(species));
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is global::Models.AntSpecies species)
            {
                var result = MessageBox.Show($"¿Seguro que quieres borrar a {species.ScientificName}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    await MainWindow.ApiClient.DeleteAsync(species.Id);
                    await RefreshDataAsync();
                }
            }
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

