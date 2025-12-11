using System.Net.Http.Headers;
using System.Net.Http.Json;
using Models;

namespace MauiApp.Services
{
    public class MauiApiClient
    {
        private readonly HttpClient _client;
        private string _token = string.Empty;
        // Ajusta IP si usas emulador Android (10.0.2.2) o dispositivo físico
        private string BaseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5000/api" : "http://localhost:5000/api";

        public MauiApiClient()
        {
            _client = new HttpClient();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try {
                var response = await _client.PostAsJsonAsync($"{BaseUrl}/auth/login", new LoginRequest { Username = username, Password = password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null)
                    {
                        _token = result.Token;
                        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                        return true;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return false;
        }

        public async Task<List<AntSpecies>> GetAllAsync()
        {
            try {
                return await _client.GetFromJsonAsync<List<AntSpecies>>($"{BaseUrl}/species") ?? new List<AntSpecies>();
            } catch { return new List<AntSpecies>(); }
        }

        public async Task<string> QueryMcpAsync(string query)
        {
             try {
                 var response = await _client.PostAsJsonAsync($"{BaseUrl}/mcp/query", new { Query = query });
                 return await response.Content.ReadAsStringAsync();
             } catch { return "Error"; }
        }
    }
}