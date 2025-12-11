using System.Configuration;
using Models;

namespace ConsoleApp
{
    class Program
    {
        static ApiClient _api = new ApiClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== ANTMASTER CONSOLE CLIENT ===");
            
            Console.Write("Usuario (admin): ");
            var user = Console.ReadLine();
            Console.Write("Password (admin): ");
            var pass = Console.ReadLine();

            if (!await _api.LoginAsync(user ?? "admin", pass ?? "admin"))
            {
                Console.WriteLine("Login fallido. Saliendo...");
                return;
            }

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n--- MENU HORMIGAS ---");
                Console.WriteLine("1. Cargar especies de prueba (Simulación Kaggle)");
                Console.WriteLine("2. Ver todas las especies");
                Console.WriteLine("3. Eliminar especie por ID");
                Console.WriteLine("4. Consulta al Mirmecólogo IA (MCP)");
                Console.WriteLine("5. Salir");
                Console.Write("Opción: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        var data = CsvLoader.LoadDummyData();
                        Console.WriteLine($"Cargando {data.Count} especies de ejemplo...");
                        await _api.UploadBatchAsync(data);
                        break;
                    case "2":
                        var list = await _api.GetAllAsync();
                        Console.WriteLine($"--- LISTADO DE ESPECIES ({list.Count}) ---");
                        foreach (var item in list.Take(20)) Console.WriteLine($"{item.Id}: {item.ScientificName} ({item.AntWikiUrl})");
                        if (list.Count > 20) Console.WriteLine("...");
                        break;
                    case "3":
                        Console.Write("ID a eliminar: ");
                        if (int.TryParse(Console.ReadLine(), out int id))
                        {
                            await _api.DeleteAsync(id);
                        }
                        break;
                    case "4":
                        Console.Write("Pregunta sobre hormigas: ");
                        var query = Console.ReadLine();
                        if (!string.IsNullOrEmpty(query))
                        {
                            var response = await _api.QueryMcpAsync(query);
                            Console.WriteLine("Respuesta Mirmecólogo: " + response);
                        }
                        break;
                    case "5":
                        exit = true;
                        break;
                }
            }
        }
    }
}