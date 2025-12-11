using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;

namespace WpfApp.Views
{
    public partial class McpView : Page
    {
        private global::Models.AntSpecies? _lastSpecies;

        public McpView()
        {
            InitializeComponent();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var query = TxtQuery.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            TxtHistory.AppendText($"Tú: {query}\n");
            TxtQuery.Text = "";

            var result = await MainWindow.ApiClient.QueryMcpAsync(query, _lastSpecies);
            TxtHistory.AppendText($"MCP: {result.Answer}\n\n");
            // Scroll to end
            TxtHistory.ScrollToEnd();

            // Mostrar foto y nombre de la primera especie asociada (si hay)
            ImgSpecies.Source = null;
            TxtSpeciesName.Text = string.Empty;
            LinkWiki.NavigateUri = null;
            LinkWiki.Inlines.Clear();

            var species = result.Species?.Find(s => !string.IsNullOrWhiteSpace(s.PhotoUrl));
            if (species != null)
            {
                try
                {
                    ImgSpecies.Source = new BitmapImage(new Uri(species.PhotoUrl, UriKind.Absolute));
                    TxtSpeciesName.Text = species.ScientificName;
                    
                    if (!string.IsNullOrEmpty(species.AntWikiUrl))
                    {
                        LinkWiki.NavigateUri = new Uri(species.AntWikiUrl);
                        LinkWiki.Inlines.Clear();
                        LinkWiki.Inlines.Add("Ver en AntWiki");
                    }
                    
                    _lastSpecies = species;
                }
                catch (Exception ex)
                {
                    ImgSpecies.Source = null;
                    TxtSpeciesName.Text = $"Error img: {ex.Message}";
                }
            }
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