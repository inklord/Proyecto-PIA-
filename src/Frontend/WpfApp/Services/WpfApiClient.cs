using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using Models = global::Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace WpfApp.Services
{
    public class WpfApiClient
    {
        private readonly HttpClient _client;
        private string _token = string.Empty;
        private const string BaseUrl = "http://localhost:5000/api"; 

        public WpfApiClient()
        {
            _client = new HttpClient();
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try {
                var response = await _client.PostAsJsonAsync($"{BaseUrl}/auth/login", new global::Models.LoginRequest { Email = email, Password = password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<global::Models.LoginResponse>();
                    if (result != null)
                    {
                        _token = result.Token;
                        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                        return true;
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("Error de conexión: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            try
            {
                var response = await _client.PostAsJsonAsync($"{BaseUrl}/auth/register", new global::Models.RegisterRequest { Email = email, Password = password });
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cuenta creada correctamente. Ahora puedes iniciar sesión.", "Registro", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"No se pudo registrar: {error}", "Registro", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión: " + ex.Message);
            }
            return false;
        }

        public async Task<List<global::Models.AntSpecies>> GetAllAsync()
        {
            try {
                return await _client.GetFromJsonAsync<List<global::Models.AntSpecies>>($"{BaseUrl}/species") ?? new List<global::Models.AntSpecies>();
            } catch { return new List<global::Models.AntSpecies>(); }
        }

        public class McpResult
        {
            public string Answer { get; set; } = string.Empty;
            public List<global::Models.AntSpecies> Species { get; set; } = new();
        }

        public async Task<McpResult> QueryMcpAsync(string query, global::Models.AntSpecies? currentSpecies = null)
        {
            try
            {
                var payload = new
                {
                    Query = query,
                    SpeciesName = currentSpecies?.ScientificName
                };

                var response = await _client.PostAsJsonAsync($"{BaseUrl}/mcp/query", payload);
                var json = await response.Content.ReadAsStringAsync();

                var result = new McpResult();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Aceptamos tanto 'Answer' como 'answer' por si el backend estuviera en otra convención
                if (root.TryGetProperty("Answer", out var answerProp) ||
                    root.TryGetProperty("answer", out answerProp))
                    result.Answer = answerProp.GetString() ?? string.Empty;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Podemos recibir 'Species' o 'Data' según el tipo de consulta
                if (root.TryGetProperty("Species", out var speciesProp) ||
                    root.TryGetProperty("species", out speciesProp))
                {
                    result.Species = JsonSerializer.Deserialize<List<global::Models.AntSpecies>>(speciesProp.GetRawText(), options) ?? new List<global::Models.AntSpecies>();
                }
                else if (root.TryGetProperty("Data", out var dataProp) ||
                         root.TryGetProperty("data", out dataProp))
                {
                    result.Species = JsonSerializer.Deserialize<List<global::Models.AntSpecies>>(dataProp.GetRawText(), options) ?? new List<global::Models.AntSpecies>();
                }

                return result;
            }
            catch (Exception ex)
            {
                return new McpResult { Answer = $"Error de conexión con MCP: {ex.Message}" };
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _client.DeleteAsync($"{BaseUrl}/species/{id}");
        }

        // Obtener descripción (species_descriptions)
        public async Task<string?> GetDescriptionAsync(int speciesId)
        {
            try
            {
                var resp = await _client.GetAsync($"{BaseUrl}/species/{speciesId}/description");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadFromJsonAsync<DescriptionDto>();
                return json?.Description;
            }
            catch
            {
                return null;
            }
        }

        private class DescriptionDto
        {
            public string Description { get; set; } = string.Empty;
        }
    }
}