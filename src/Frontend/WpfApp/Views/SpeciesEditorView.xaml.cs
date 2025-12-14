using System.Windows;
using System.Windows.Controls;
using Models;

namespace WpfApp.Views
{
    public partial class SpeciesEditorView : Page
    {
        private AntSpecies? _species;

        public SpeciesEditorView(AntSpecies? species = null)
        {
            InitializeComponent();
            _species = species;

            if (_species != null)
            {
                TxtName.Text = _species.ScientificName;
                TxtWiki.Text = _species.AntWikiUrl;
                TxtPhoto.Text = _species.PhotoUrl;
                TxtInat.Text = _species.InaturalistId;
                TxtDesc.Text = _species.Description;
                TxtPhoto_TextChanged(null, null);
            }
        }

        private void TxtPhoto_TextChanged(object sender, TextChangedEventArgs? e)
        {
            if (string.IsNullOrWhiteSpace(TxtPhoto.Text))
            {
                if (ImgPreview != null) ImgPreview.Source = null;
                return;
            }
            try
            {
                var uri = new System.Uri(TxtPhoto.Text, System.UriKind.Absolute);
                if (ImgPreview != null) ImgPreview.Source = new System.Windows.Media.Imaging.BitmapImage(uri);
            }
            catch { if (ImgPreview != null) ImgPreview.Source = null; }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            if (_species == null)
            {
                // Crear
                var newSpec = new AntSpecies
                {
                    ScientificName = TxtName.Text,
                    AntWikiUrl = TxtWiki.Text,
                    PhotoUrl = TxtPhoto.Text,
                    InaturalistId = TxtInat.Text,
                    Description = TxtDesc.Text
                };
                if (await MainWindow.ApiClient.CreateAsync(newSpec))
                {
                    MessageBox.Show("Especie creada.");
                    NavigationService.GoBack();
                }
            }
            else
            {
                // Editar
                _species.ScientificName = TxtName.Text;
                _species.AntWikiUrl = TxtWiki.Text;
                _species.PhotoUrl = TxtPhoto.Text;
                _species.InaturalistId = TxtInat.Text;
                _species.Description = TxtDesc.Text;

                if (await MainWindow.ApiClient.UpdateAsync(_species))
                {
                    MessageBox.Show("Especie actualizada.");
                    NavigationService.GoBack();
                }
            }
        }
    }
}
