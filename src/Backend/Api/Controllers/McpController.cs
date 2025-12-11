using Microsoft.AspNetCore.Mvc;
using Persistence;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class McpController : ControllerBase
    {
        private readonly IRepository<Models.AntSpecies> _repository = RepositoryFactory.GetRepository();
        private readonly IConfiguration _configuration;

        public McpController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string OpenAiKey =>
            _configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? string.Empty;

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] McpQueryRequest request)
        {
            var originalQuery = request.Query ?? string.Empty;
            var query = originalQuery.ToLower().Trim();
            var speciesNameContext = request.SpeciesName?.Trim();

            var data = (await _repository.GetAllAsync()).ToList();

            // 0) Petición directa de mostrar una especie concreta ("muéstrame X", "muestrame X", "enséñame X")
            if (query.Contains("muestrame") || query.Contains("muéstrame") ||
                query.Contains("enseñame") || query.Contains("enséñame") ||
                query.Contains("muestrame una") || query.Contains("muéstrame una"))
            {
                var speciesWithNames = data.Where(s => !string.IsNullOrWhiteSpace(s.ScientificName)).ToList();

                // 0.1 Intentar encontrar coincidencias exactas de nombre científico ("Messor barbarus")
                var fullMatches = speciesWithNames
                    .Where(s => query.Contains(s.ScientificName.ToLower()))
                    .Where(s => !string.IsNullOrWhiteSpace(s.PhotoUrl))
                    .ToList();

                // 0.2 Si no hay coincidencias exactas, caer a coincidencias por género
                List<Models.AntSpecies> directMatches;
                if (fullMatches.Any())
                {
                    directMatches = fullMatches;
                }
                else
                {
                    directMatches = speciesWithNames
                        .Where(s =>
                        {
                            var genus = s.ScientificName.Split(' ')[0].ToLower();
                            return query.Contains(genus);
                        })
                        .Where(s => !string.IsNullOrWhiteSpace(s.PhotoUrl))
                        .ToList();
                }

                if (directMatches.Any())
                {
                    return Ok(new
                    {
                        Answer = $"He encontrado {directMatches.Count} especies que coinciden con tu petición. Aquí tienes algunos ejemplos:",
                        Species = directMatches
                    });
                }
            }

            // 1) Preguntas de estadísticas globales
            if (query.Contains("cuantas") || query.Contains("total") || query.Contains("cuántas"))
            {
                var withPhoto = data.Count(d => !string.IsNullOrWhiteSpace(d.PhotoUrl));
                return Ok(new
                {
                    Answer = $"Tenemos {data.Count} especies registradas en AntMaster, de las cuales {withPhoto} tienen foto asociada.",
                    Total = data.Count,
                    ConFoto = withPhoto
                });
            }

            // 2) Búsqueda / filtro por género (buscador natural)
            if (query.Contains("buscar") || query.Contains("muestr") || query.Contains("ver ") || query.Contains("especies de"))
            {
                var genera = data
                    .Where(s => !string.IsNullOrWhiteSpace(s.ScientificName))
                    .Select(s => s.ScientificName.Split(' ')[0])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var matchedGenera = genera
                    .Where(g => query.Contains(g.ToLower()))
                    .ToList();

                if (matchedGenera.Any())
                {
                    var result = data
                        .Where(s => matchedGenera.Contains(s.ScientificName.Split(' ')[0], StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    return Ok(new
                    {
                        Answer = $"He encontrado {result.Count} especies de los géneros: {string.Join(", ", matchedGenera)}.",
                        Data = result
                    });
                }
            }

            // 3) Búsqueda de especies similares
            if (query.Contains("parecidas a") || query.Contains("similares a"))
            {
                // Tomamos todo lo que va después de 'a '
                var idx = query.LastIndexOf(" a ");
                if (idx >= 0 && idx + 3 < query.Length)
                {
                    var term = query[(idx + 3)..].Trim();
                    var similars = data
                        .Where(s => s.ScientificName.ToLower().Contains(term))
                        .ToList();

                    if (similars.Any())
                    {
                        return Ok(new
                        {
                            Answer = $"Estas especies coinciden con el término '{term}':",
                            Data = similars
                        });
                    }
                }
            }

            // Si tenemos una especie en contexto (p.ej. seleccionada en la UI), se la indicamos explícitamente al LLM
            // PERO le indicamos que si el usuario pregunta por otra, ignore este contexto.
            var llmQuery = string.IsNullOrEmpty(speciesNameContext)
                ? originalQuery
                : $"Contexto previo: se estaba visualizando '{speciesNameContext}'. Pregunta del usuario: '{originalQuery}'. (Si la pregunta menciona explícitamente otra especie distinta, responde sobre la nueva).";

            return await ProcessWithOpenAi(llmQuery, data);
        }

        private async Task<IActionResult> ProcessWithOpenAi(string userQuery, IEnumerable<Models.AntSpecies> contextData)
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(OpenAiKey))
                {
                    return StatusCode(500, new { error = "OPENAI_API_KEY no está configurada en el entorno del servidor." });
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OpenAiKey);

                var allSpecies = contextData.ToList();

                var sampleData = allSpecies.Take(20).Select(x => x.ScientificName).ToList();
                var stats = $"Total especies en BD: {allSpecies.Count}.";

                var systemPrompt = @$"
Eres un mirmecólogo experto (experto en hormigas) y actúas como asistente para una app tipo iNaturalist centrada en hormigas.

Tienes acceso lógico SOLO a una base de datos llamada 'AntMaster' con nombres científicos de especies de hormigas.
Contexto rápido: {stats}
Algunos ejemplos de especies presentes: {string.Join(", ", sampleData)}.

REGLAS MUY IMPORTANTES:
- Solo para verificar la EXISTENCIA de una especie usa estrictamente la base de datos AntMaster.
- Para guías de crianza, alimentación, comportamiento, biología y cuidados generales, PUEDES y DEBES usar tu conocimiento general experto, aunque la especie no esté detallada en la BD.
- No afirmes nunca con seguridad que una especie está o no está en España ni en ningún país concreto basándote solo en la BD, pero puedes mencionar su distribución habitual según tu conocimiento experto.
- Para recomendaciones de especies para principiantes, prioriza especies de géneros comunes y fáciles de mantener (por ejemplo Lasius, Messor, Camponotus).
- Si te pasamos una lista filtrada de especies (por ejemplo, de un género), céntrate en esas especies y no menciones otras.

Responde SIEMPRE en español, de forma clara y EXPERTA. No temas extenderte en las explicaciones si es necesario para dar una guía completa (hasta 4-5 párrafos). 
Si no estás seguro de algo, dilo explícitamente y sugiere al usuario que consulte fuentes adicionales.";

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userQuery }
                    },
                    max_tokens = 1000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = new { error = $"Fallo al llamar a OpenAI: {response.StatusCode}", body = responseString };
                    return StatusCode((int)response.StatusCode, errorObj);
                }

                using var doc = JsonDocument.Parse(responseString);
                var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

                // Detectar especies mencionadas en la PREGUNTA del usuario (priorizando coincidencia exacta
                // y, si no existe, la especie científicamente más cercana al texto introducido).
                var lowerQuery = userQuery.ToLower();

                var candidateQuerySpecies = allSpecies
                    .Where(s => !string.IsNullOrWhiteSpace(s.ScientificName))
                    .Where(s => !string.IsNullOrWhiteSpace(s.PhotoUrl))
                    .Select(s => new
                    {
                        Species = s,
                        NameLower = s.ScientificName.ToLower(),
                        GenusLower = s.ScientificName.Split(' ')[0].ToLower()
                    })
                    .Where(x => lowerQuery.Contains(x.NameLower) || lowerQuery.Contains(x.GenusLower))
                    .ToList();

                List<Models.AntSpecies> matchedSpecies = new();

                if (candidateQuerySpecies.Any())
                {
                    // 1) Si el usuario ha escrito EXACTAMENTE el nombre científico, priorizamos esas coincidencias.
                    var exactMatches = candidateQuerySpecies
                        .Where(x => lowerQuery.Contains(x.NameLower))
                        .OrderByDescending(x => x.NameLower.Length)
                        .Select(x => x.Species)
                        .ToList();

                    if (exactMatches.Any())
                    {
                        matchedSpecies = exactMatches.Take(5).ToList();
                    }
                    else
                    {
                        // 2) Si solo coincide el género, usamos una métrica de similitud (distancia de Levenshtein)
                        //    entre el texto que escribió el usuario y el nombre científico completo para elegir
                        //    la especie "más parecida" a la petición.

                        // Intentamos extraer de la pregunta la parte que parece "nombre de especie":
                        // buscamos el primer género mencionado y tomamos ese término + la siguiente palabra.
                        string requestedName = lowerQuery;
                        var firstGenus = candidateQuerySpecies.Select(x => x.GenusLower).FirstOrDefault(g => lowerQuery.Contains(g));
                        if (!string.IsNullOrWhiteSpace(firstGenus))
                        {
                            var idx = lowerQuery.IndexOf(firstGenus, StringComparison.Ordinal);
                            if (idx >= 0)
                            {
                                var tail = lowerQuery[idx..];
                                var stopChars = new[] { '.', '?', '!', '\n' };
                                var stopIdx = tail.IndexOfAny(stopChars);
                                if (stopIdx >= 0) tail = tail[..stopIdx];

                                var words = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (words.Length >= 2)
                                    requestedName = $"{words[0]} {words[1]}";
                                else
                                    requestedName = words[0];
                            }
                        }

                        matchedSpecies = candidateQuerySpecies
                            .OrderBy(x => ComputeLevenshteinDistance(requestedName, x.NameLower))
                            .ThenByDescending(x => x.NameLower.Length)
                            .Select(x => x.Species)
                            .Take(5)
                            .ToList();
                    }
                }

                // Si el usuario pregunta por species para empezar / principiantes y no hemos detectado ninguna,
                // seleccionamos algunas especies recomendadas conocidas con foto.
                if (!matchedSpecies.Any() &&
                    (lowerQuery.Contains("empezar") || lowerQuery.Contains("principiante") || lowerQuery.Contains("iniciarme")))
                {
                    var recommended = new[] { "Lasius niger", "Messor barbarus", "Camponotus cruentatus" };
                    matchedSpecies = allSpecies
                        .Where(s => recommended.Contains(s.ScientificName, StringComparer.OrdinalIgnoreCase)
                                    && !string.IsNullOrWhiteSpace(s.PhotoUrl))
                        .ToList();
                }

                return Ok(new
                {
                    Answer = answer,
                    Species = matchedSpecies
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Excepción interna al llamar a OpenAI: {ex.Message}" });
            }
        }
        /// <summary>
        /// Calcula la distancia de Levenshtein entre dos cadenas (medida clásica de "parecido" entre textos).
        /// </summary>
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

    public class McpQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        // Opcionalmente se podría enviar el nombre de la especie actual o una lista de IDs,
        // para mejorar el contexto del asistente en futuras ampliaciones.
        public string? SpeciesName { get; set; }
        public List<int>? SpeciesIds { get; set; }
    }
}