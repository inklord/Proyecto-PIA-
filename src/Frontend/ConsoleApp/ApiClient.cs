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

        public async Task<bool> RegisterAsync(string email, string password)
        {
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/auth/register", new LoginRequest { Email = email, Password = password });
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Usuario registrado correctamente.");
                return true;
            }
            Console.WriteLine($"Error al registrar: {response.StatusCode}");
            return false;
        }

        public async Task<List<AntSpecies>> GetAllAsync()
        {
            return await _client.GetFromJsonAsync<List<AntSpecies>>($"{BaseUrl}/species") ?? new List<AntSpecies>();
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _client.DeleteAsync($"{BaseUrl}/species/{id}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Eliminado." : "Error al eliminar.");
        }

        public async Task CreateAsync(AntSpecies species)
        {
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/species", species);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Creado correctamente.");
            }
            else
            {
                // Leer el error del servidor (que ahora incluye la excepción SQL)
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al crear ({response.StatusCode}): {errorMsg}");
            }
        }

        public async Task UpdateAsync(AntSpecies species)
        {
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/species", species);
            Console.WriteLine(response.IsSuccessStatusCode ? "Actualizado correctamente." : $"Error al actualizar: {response.StatusCode}");
        }

        public async Task<string> QueryMcpAsync(string query)
        {
             // La consola usa la versión simplificada, aquí podríamos implementar WS si fuera necesario
             // Por ahora devolvemos un mensaje de que use WPF/MAUI para el chat avanzado
             return await Task.FromResult("El chat por consola está deshabilitado. Usa WPF o MAUI para hablar con la IA.");
        }
    }
}