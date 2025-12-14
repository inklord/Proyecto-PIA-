using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Models;
using System.Threading.Tasks;

namespace WpfApp.Views
{
    public partial class AdminView : Page
    {
        private List<AntSpecies> _allSpecies = new();

        public AdminView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            _allSpecies = await MainWindow.ApiClient.GetAllAsync();
            FilterData();
        }

        private void FilterData()
        {
            var txt = TxtSearch.Text?.ToLower() ?? "";
            var filtered = _allSpecies
                .Where(s => string.IsNullOrWhiteSpace(txt) || 
                           (s.ScientificName?.ToLower().Contains(txt) == true))
                .ToList();
            GridSpecies.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SpeciesEditorView());
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is AntSpecies species)
            {
                NavigationService.Navigate(new SpeciesEditorView(species));
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is AntSpecies species)
            {
                if (MessageBox.Show($"¿Eliminar {species.ScientificName}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await MainWindow.ApiClient.DeleteAsync(species.Id);
                    await LoadData();
                }
            }
        }
    }
}
