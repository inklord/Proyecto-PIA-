using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Persistence;
using System.Net.Http.Headers;

namespace Api.Services
{
    public class McpWebSocketHandler
    {
        private static readonly JsonSerializerOptions RpcJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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
                        var respJson = JsonSerializer.Serialize(response, RpcJsonOptions);
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
            if (query.Contains("muestrame") || query.Contains("muéstrame") || query.Contains("enseñame") || query.Contains("enséñame")
                || query.Contains("mostrar") || query.Contains("ver ") || query.Contains("busca") || query.Contains("buscar"))
            {
                // 0.1 Intentar coincidencia exacta por nombre científico completo dentro del texto
                var exact = data.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.ScientificName) &&
                    query.Contains(s.ScientificName.ToLower()));

                if (exact != null)
                {
                    return new ExpertResult 
                    { 
                        Answer = $"Aquí tienes a {exact.ScientificName}.", 
                        Species = new List<Models.AntSpecies> { exact } 
                    };
                }

                // 0.2 Si no hay coincidencia exacta, intentar sugerencias por similitud (typos)
                var requested = ExtractPossibleSpeciesName(userQuery);
                if (!string.IsNullOrWhiteSpace(requested))
                {
                    var suggestions = FindSimilarSpecies(requested, data, threshold: 60).Take(5).ToList();
                    if (suggestions.Any())
                    {
                        var lines = suggestions.Select(s => $"- {s.Species.ScientificName} ({s.Similarity:0.0}%)");
                        return new ExpertResult
                        {
                            Answer =
                                $"No encontré exactamente '{requested}'. ¿Quizás te refieres a alguna de estas especies?\n\n" +
                                string.Join("\n", lines),
                            Species = suggestions.Select(s => s.Species).ToList()
                        };
                    }
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
            // Si el usuario escribe mal una especie, intentamos recuperar las más cercanas por similitud.
            // Esto mejora mucho el UX (typos, acentos, etc.).
            var requested = ExtractPossibleSpeciesName(query);
            if (!string.IsNullOrWhiteSpace(requested))
            {
                return FindSimilarSpecies(requested, all, threshold: 55)
                    .Take(5)
                    .Select(x => x.Species)
                    .ToList();
            }

            var lower = query.ToLowerInvariant();
            return all
                .Where(s => !string.IsNullOrWhiteSpace(s.ScientificName))
                .Where(s => lower.Contains(s.ScientificName.ToLowerInvariant()) ||
                            lower.Contains(s.ScientificName.Split(' ')[0].ToLowerInvariant()))
                .Take(5)
                .ToList();
        }

        private sealed record SimilarSpecies(Models.AntSpecies Species, double Similarity);

        /// <summary>
        /// Extrae de una frase lo que "parece" un nombre de especie (heurística).
        /// Ej: "enséñame una myrmecia nigrocincta" -> "myrmecia nigrocincta"
        /// </summary>
        private static string ExtractPossibleSpeciesName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lower = input.ToLowerInvariant();

            // Quitamos signos y normalizamos espacios
            lower = lower.Replace("¿", " ").Replace("?", " ").Replace("!", " ").Replace(".", " ")
                         .Replace(",", " ").Replace(":", " ").Replace(";", " ")
                         .Replace("\n", " ").Replace("\r", " ");
            lower = NormalizeSpaces(lower);

            // Si hay palabras tipo "muéstrame/enséñame/buscar/ver", tomamos lo que va después
            var triggers = new[] { "muestrame", "muéstrame", "enseñame", "enséñame", "mostrar", "ver", "buscar", "busca" };
            foreach (var t in triggers)
            {
                var idx = lower.IndexOf(t, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var tail = lower[(idx + t.Length)..].Trim();
                    tail = tail.TrimStart(new[] { ' ', 'a', 'u', 'n', 'a' }).Trim(); // heurística rápida
                    tail = NormalizeSpaces(tail);
                    var words = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 2) return $"{words[0]} {words[1]}";
                    if (words.Length == 1) return words[0];
                }
            }

            // Si no, intentamos sacar las dos últimas palabras si parecen "genus species"
            var parts = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[^2]} {parts[^1]}";
            return parts.Length == 1 ? parts[0] : string.Empty;
        }

        private static string NormalizeSpaces(string s)
            => string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Encuentra especies similares por nombre científico usando varias heurísticas.
        /// Similarity: 0..100.
        /// </summary>
        private static IEnumerable<SimilarSpecies> FindSimilarSpecies(string input, IEnumerable<Models.AntSpecies> all, int threshold)
        {
            var needle = input.ToLowerInvariant().Trim();
            if (string.IsNullOrWhiteSpace(needle)) yield break;

            foreach (var s in all)
            {
                if (string.IsNullOrWhiteSpace(s.ScientificName)) continue;
                var hay = s.ScientificName.ToLowerInvariant().Trim();

                // Exact match
                double exact = hay == needle ? 100 : 0;
                // Contains (muy útil cuando solo ponen género o parte del epíteto)
                double contains = (hay.Contains(needle) || needle.Contains(hay)) ? 90 : 0;
                // Levenshtein similarity
                double lev = LevenshteinSimilarityPercent(needle, hay);

                var sim = Math.Max(exact, Math.Max(contains, lev));
                if (sim >= threshold)
                    yield return new SimilarSpecies(s, sim);
            }
        }

        private static double LevenshteinSimilarityPercent(string a, string b)
        {
            var dist = ComputeLevenshteinDistance(a, b);
            var max = Math.Max(a.Length, b.Length);
            if (max == 0) return 100;
            return 100.0 * (1.0 - (double)dist / max);
        }

        private static int ComputeLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var n = source.Length;
            var m = target.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
