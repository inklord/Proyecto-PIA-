using System.Net.Http.Headers;
using System.Net.Http.Json;
using Models;
using Newtonsoft.Json;

namespace ConsoleApp
{
    public class ApiClient
    {
        private readonly HttpClient _client;
        private string _token = string.Empty;
        private const string BaseUrl = "http://localhost:5000/api";

        public ApiClient()
        {
            _client = new HttpClient();
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/auth/login", new LoginRequest { Email = email, Password = password });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _token = result.Token;
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    Console.WriteLine($"Login exitoso. Token expira: {result.Expiration}");
                    return true;
                }
            }
            return false;
        }

        public async Task UploadBatchAsync(List<AntSpecies> data)
        {
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/species/batch", data); // Endpoint cambiado
            if (response.IsSuccessStatusCode) Console.WriteLine("Carga masiva de hormigas completada.");
            else Console.WriteLine($"Error en carga: {response.StatusCode}");
        }

        public async Task<List<AntSpecies>> GetAllAsync()
        {
            return await _client.GetFromJsonAsync<List<AntSpecies>>($"{BaseUrl}/species") ?? new List<AntSpecies>(); // Endpoint cambiado
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _client.DeleteAsync($"{BaseUrl}/species/{id}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Eliminado." : "Error al eliminar.");
        }

        public async Task<string> QueryMcpAsync(string query)
        {
             var response = await _client.PostAsJsonAsync($"{BaseUrl}/mcp/query", new { Query = query });
             var content = await response.Content.ReadAsStringAsync();
             return content;
        }
    }
}