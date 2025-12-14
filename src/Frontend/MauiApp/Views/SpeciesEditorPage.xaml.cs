using Models;

namespace MauiApp.Views
{
    public partial class SpeciesEditorPage : ContentPage
    {
        private AntSpecies? _species;

        public SpeciesEditorPage(AntSpecies? species = null)
        {
            InitializeComponent();
            _species = species;

            if (_species != null)
            {
                Title = "Editar Especie";
                TxtName.Text = _species.ScientificName;
                TxtUrl.Text = _species.AntWikiUrl;
            }
            else
            {
                Title = "Nueva Especie";
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                await DisplayAlert("Error", "El nombre es obligatorio", "OK");
                return;
            }

            if (_species == null)
            {
                // Crear nueva
                var newSpecies = new AntSpecies 
                { 
                    ScientificName = TxtName.Text, 
                    AntWikiUrl = TxtUrl.Text 
                };
                await App.ApiClient.CreateAsync(newSpecies);
            }
            else
            {
                // Editar existente
                _species.ScientificName = TxtName.Text;
                _species.AntWikiUrl = TxtUrl.Text;
                await App.ApiClient.UpdateAsync(_species);
            }

            await Navigation.PopAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
