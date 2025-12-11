using Models = global::Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using WpfApp.Models;

namespace WpfApp.Views
{
    public partial class MasterView : Page
    {
        private List<GenusGroup> _groups = new();

        public MasterView()
        {
            InitializeComponent();
            Loaded += MasterView_Loaded;
        }

        private async void MasterView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            var data = await MainWindow.ApiClient.GetAllAsync();

            // Filtramos solo especies que tengan foto válida
            var withPhotos = data
                .Where(s => !string.IsNullOrWhiteSpace(s.ScientificName)
                            && !string.IsNullOrWhiteSpace(s.PhotoUrl))
                .ToList();

            _groups = withPhotos
                .GroupBy(s => s.ScientificName.Split(' ')[0])
                .OrderBy(g => g.Key)
                .Select(g => new GenusGroup
                {
                    Genus = g.Key,
                    Species = g.ToList(),
                    RepresentativePhotoUrl = g.First().PhotoUrl
                })
                .ToList();

            GenusItems.ItemsSource = _groups;
        }

        private async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private void GenusCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GenusGroup group)
            {
                NavigationService.Navigate(new GenusView(group));
            }
        }
    }
}