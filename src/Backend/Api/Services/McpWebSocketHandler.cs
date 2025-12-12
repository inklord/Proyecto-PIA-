using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Persistence;
using System.Net.Http.Headers;

namespace Api.Services
{
    public class McpWebSocketHandler
    {
        private readonly IRepository<Models.AntSpecies> _repository;
        private readonly IConfiguration _configuration;

        public McpWebSocketHandler(IRepository<Models.AntSpecies> repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        private string OpenAiKey =>
            _configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? string.Empty;

        public async Task HandleAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 8]; // Buffer más grande para respuestas de IA

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var message = await ReceiveFullTextMessage(webSocket, buffer);
                    if (message == null)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cerrado por el servidor", CancellationToken.None);
                        break;
                    }

                    Console.WriteLine($"[MCP] << {message}");
                    var response = await ProcessMessageAsync(message);
                    if (response != null)
                    {
                        var respJson = JsonSerializer.Serialize(response);
                        Console.WriteLine($"[MCP] >> {respJson}");
                        var bytes = Encoding.UTF8.GetBytes(respJson);
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Error en WebSocket MCP: {ex}");
            }
        }

        private static async Task<string?> ReceiveFullTextMessage(WebSocket ws, byte[] buffer)
        {
            var sb = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                if (result.MessageType != WebSocketMessageType.Text) continue;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            return sb.ToString();
        }

        private async Task<JsonRpcResponse> ProcessMessageAsync(string json)
        {
            JsonRpcRequest request;
            try
            {
                request = JsonSerializer.Deserialize<JsonRpcRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return new JsonRpcResponse { Error = new JsonRpcError { Code = -32700, Message = "Parse error" } };
            }

            if (request == null || string.IsNullOrEmpty(request.Method))
                return new JsonRpcResponse { Id = request?.Id, Error = new JsonRpcError { Code = -32600, Message = "Invalid Request" } };

            switch (request.Method)
            {
                case "initialize":
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { tools = new { }, resources = new { } },
                            serverInfo = new { name = "AntMaster-MCP", version = "1.0.0" }
                        }
                    };

                case "tools/list":
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new
                        {
                            tools = new[]
                            {
                                new
                                {
                                    name = "ask_expert",
                                    description = "Consulta experta sobre hormigas con IA y contexto de la BD.",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            query = new { type = "string", description = "Pregunta del usuario." },
                                            speciesContext = new { type = "string", description = "Nombre de la especie actual (opcional)." }
                                        },
                                        required = new[] { "query" }
                                    }
                                }
                            }
                        }
                    };

                case "tools/call":
                    return await HandleToolCall(request);

                default:
                    return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32601, Message = "Method not found" } };
            }
        }

        private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
        {
            try
            {
                var element = (JsonElement)request.Params;
                if (!element.TryGetProperty("name", out var nameProp))
                     return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32602, Message = "Missing tool name" } };

                var toolName = nameProp.GetString();

                if (toolName == "ask_expert")
                {
                    string query = "";
                    string? speciesContext = null;

                    if (element.TryGetProperty("arguments", out var argsProp))
                    {
                        if (argsProp.TryGetProperty("query", out var qProp)) query = qProp.GetString() ?? "";
                        if (argsProp.TryGetProperty("speciesContext", out var sProp)) speciesContext = sProp.GetString();
                    }

                    // Lógica RAG completa
                    var resultObj = await ProcessExpertQuery(query, speciesContext);
                    
                    // Empaquetamos la respuesta compleja como JSON string dentro del contenido de texto
                    // para cumplir con la estructura de ToolResult de MCP.
                    var jsonResult = JsonSerializer.Serialize(resultObj);

                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new
                        {
                            content = new[]
                            {
                                new { type = "text", text = jsonResult }
                            }
                        }
                    };
                }

                return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32601, Message = $"Tool '{toolName}' not found" } };
            }
            catch (Exception ex)
            {
                return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32000, Message = ex.Message } };
            }
        }

        // --- LÓGICA DE NEGOCIO (Migrada desde McpController) ---

        private class ExpertResult
        {
            public string Answer { get; set; } = string.Empty;
            public List<Models.AntSpecies> Species { get; set; } = new();
            public string? Error { get; set; }
        }

        private async Task<ExpertResult> ProcessExpertQuery(string userQuery, string? speciesNameContext)
        {
            var query = userQuery.ToLower().Trim();
            var data = (await _repository.GetAllAsync()).ToList();

            // 0) Peticiones directas
            if (query.Contains("muestrame") || query.Contains("muéstrame") || query.Contains("enseñame"))
            {
                // Lógica simplificada de coincidencia exacta para este ejemplo
                var exact = data.FirstOrDefault(s => query.Contains(s.ScientificName.ToLower()));
                if (exact != null)
                {
                    return new ExpertResult 
                    { 
                        Answer = $"Aquí tienes a {exact.ScientificName}.", 
                        Species = new List<Models.AntSpecies> { exact } 
                    };
                }
            }

            // Llamada a OpenAI
            return await CallOpenAi(userQuery, speciesNameContext, data);
        }

        private async Task<ExpertResult> CallOpenAi(string userQuery, string? contextName, List<Models.AntSpecies> allSpecies)
        {
            if (string.IsNullOrWhiteSpace(OpenAiKey))
                return new ExpertResult { Answer = "Error: OPENAI_API_KEY no configurada en el servidor." };

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiKey);

                var sampleData = allSpecies.Take(20).Select(x => x.ScientificName).ToList();
                var stats = $"Total especies en BD: {allSpecies.Count}.";
                
                var systemPrompt = $"Eres un experto en hormigas. Responde en español. Contexto BD: {stats}. Ejemplos: {string.Join(", ", sampleData)}.";
                var finalQuery = string.IsNullOrEmpty(contextName) ? userQuery : $"Contexto: {contextName}. Pregunta: {userQuery}";

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = finalQuery }
                    },
                    max_tokens = 800
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                    return new ExpertResult { Answer = $"Error OpenAI: {response.StatusCode}" };

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                // Lógica de búsqueda borrosa de especies relacionadas
                var matched = FindRelatedSpecies(userQuery, allSpecies);

                return new ExpertResult { Answer = answer, Species = matched };
            }
            catch (Exception ex)
            {
                return new ExpertResult { Answer = $"Excepción: {ex.Message}" };
            }
        }

        private List<Models.AntSpecies> FindRelatedSpecies(string query, List<Models.AntSpecies> all)
        {
            var lower = query.ToLower();
            // Lógica simple por ahora para no extender demasiado el código
            return all.Where(s => lower.Contains(s.ScientificName.ToLower()) || lower.Contains(s.ScientificName.Split(' ')[0].ToLower()))
                      .Take(5).ToList();
        }
    }
}
