using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace WpfApp.Views
{
    public partial class SpeciesDetailView : Page
    {
        private readonly global::Models.AntSpecies _species;

        public SpeciesDetailView(global::Models.AntSpecies species, string? description)
        {
            InitializeComponent();
            _species = species;

            TxtTitle.Text = species.ScientificName;
            TxtScientificName.Text = species.ScientificName;

            // Imagen
            if (!string.IsNullOrWhiteSpace(species.PhotoUrl))
            {
                try
                {
                    ImgSpecies.Source = new BitmapImage(new Uri(species.PhotoUrl, UriKind.Absolute));
                }
                catch
                {
                    ImgSpecies.Source = null;
                }
            }

            // Enlace AntWiki
            if (!string.IsNullOrWhiteSpace(species.AntWikiUrl))
            {
                try
                {
                    LinkWiki.NavigateUri = new Uri(species.AntWikiUrl);
                }
                catch
                {
                    LinkWiki.NavigateUri = null;
                }
            }

            // Descripción: limpiamos un poco el markdown (**nombre**)
            var text = description ?? "No hay descripción registrada para esta especie.";
            text = text.Replace("**", string.Empty);
            TxtDescription.Text = text;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void ImgSpecies_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ImgSpecies.Source == null) return;

            var win = new Window
            {
                Title = _species.ScientificName,
                Background = System.Windows.Media.Brushes.Black,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Width = 800,
                Height = 600,
                Content = new ScrollViewer
                {
                    Content = new System.Windows.Controls.Image
                    {
                        Source = ImgSpecies.Source,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    }
                }
            };

            win.ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el enlace: {ex.Message}");
            }
        }
    }
}


