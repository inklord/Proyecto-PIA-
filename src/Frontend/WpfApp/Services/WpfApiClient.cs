using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace WpfApp.Services
{
    public class WpfApiClient
    {
        private readonly HttpClient _client;
        private string _token = string.Empty;
        // Cambia localhost:5000 por tu puerto real si es diferente
        private const string BaseUrl = "http://localhost:5000/api";
        private const string WsUrl = "ws://localhost:5000/mcp"; 

        private ClientWebSocket? _ws;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<McpResult>> _pendingRequests = new();
        private CancellationTokenSource? _cts;

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

        // --- LÓGICA WEBSOCKET MCP (JSON-RPC) ---

        public async Task<McpResult> QueryMcpAsync(string query, global::Models.AntSpecies? currentSpecies = null)
        {
            try
            {
                await EnsureConnected();

                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<McpResult>();
                _pendingRequests.TryAdd(requestId, tcs);

                // Construimos petición JSON-RPC tools/call
                var request = new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    method = "tools/call",
                    @params = new
                    {
                        name = "ask_expert",
                        arguments = new
                        {
                            query = query,
                            speciesContext = currentSpecies?.ScientificName
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    return new McpResult { Answer = "Error: Conexión WebSocket cerrada." };
                }

                // Esperar respuesta (con timeout de 30s)
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(30000));
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    _pendingRequests.TryRemove(requestId, out _);
                    return new McpResult { Answer = "Error: Timeout esperando al experto." };
                }
            }
            catch (Exception ex)
            {
                return new McpResult { Answer = $"Error interno MCP: {ex.Message}" };
            }
        }

        private async Task EnsureConnected()
        {
            if (_ws != null && _ws.State == WebSocketState.Open) return;

            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            try
            {
                await _ws.ConnectAsync(new Uri(WsUrl), _cts.Token);
                // Iniciar bucle de escucha
                _ = Task.Run(() => ListenLoop(_ws, _cts.Token));
                
                // Opcional: Enviar initialize (según protocolo MCP)
                // Pero para simplificar pasamos directo a tools/call
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar al servidor MCP ({WsUrl}): {ex.Message}");
            }
        }

        private async Task ListenLoop(ClientWebSocket ws, CancellationToken token)
        {
            var buffer = new byte[1024 * 8];
            try
            {
                while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var msg = await ReceiveFullTextMessage(ws, buffer, token);
                    if (msg == null) break;
                    HandleMessage(msg);
                }
            }
            catch
            {
                // Ignorar errores de desconexión
            }
        }

        private static async Task<string?> ReceiveFullTextMessage(ClientWebSocket ws, byte[] buffer, CancellationToken token)
        {
            var sb = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                if (result.MessageType != WebSocketMessageType.Text) continue;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            return sb.ToString();
        }

        private void HandleMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // Verificar ID
                string? id = null;
                if (root.TryGetProperty("id", out var idProp)) 
                {
                    if (idProp.ValueKind == JsonValueKind.String) id = idProp.GetString();
                    else if (idProp.ValueKind == JsonValueKind.Number) id = idProp.ToString();
                }

                if (id != null && _pendingRequests.TryRemove(id, out var tcs))
                {
                    // Es una respuesta a nuestra petición
                    if (root.TryGetProperty("error", out var errProp))
                    {
                         var errMsg = errProp.GetProperty("message").GetString();
                         tcs.SetResult(new McpResult { Answer = $"Error del Servidor: {errMsg}" });
                    }
                    else if (root.TryGetProperty("result", out var resProp))
                    {
                        // En MCP la respuesta de una tool viene en result.content[0].text
                        // El servidor nos envía el JSON serializado ahí dentro.
                        if (resProp.TryGetProperty("content", out var contentProp) && contentProp.GetArrayLength() > 0)
                        {
                            var textBlock = contentProp[0];
                            var innerJson = textBlock.GetProperty("text").GetString();

                            if (!string.IsNullOrEmpty(innerJson))
                            {
                                var finalResult = JsonSerializer.Deserialize<McpResult>(innerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                tcs.SetResult(finalResult ?? new McpResult { Answer = "Respuesta vacía." });
                            }
                            else
                            {
                                tcs.SetResult(new McpResult { Answer = "Contenido vacío." });
                            }
                        }
                        else
                        {
                             tcs.SetResult(new McpResult { Answer = "Formato de respuesta desconocido." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing WS msg: " + ex.Message);
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _client.DeleteAsync($"{BaseUrl}/species/{id}");
        }

        public async Task<string?> GetDescriptionAsync(int speciesId)
        {
            try
            {
                var resp = await _client.GetAsync($"{BaseUrl}/species/{speciesId}/description");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadFromJsonAsync<DescriptionDto>();
                return json?.Description;
            }
            catch { return null; }
        }

        private class DescriptionDto
        {
            public string Description { get; set; } = string.Empty;
        }
    }
}
