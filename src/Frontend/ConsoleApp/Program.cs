using System.Configuration;
using Models;

namespace ConsoleApp
{
    class Program
    {
        static ApiClient _api = new ApiClient();

        static async Task Main(string[] args)
        {
            try 
            {
                Console.Title = "AntMaster Admin Console";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("========================================");
                Console.WriteLine("   ANTMASTER - GESTIÓN ADMINISTRATIVA   ");
                Console.WriteLine("========================================");
                Console.ResetColor();
                
                Console.Write("\nUsuario (admin): ");
                var user = Console.ReadLine();
                Console.Write("Password: ");
                var pass = ReadPassword(); // Ocultar password

                Console.WriteLine("\nConectando...");
                if (!await _api.LoginAsync(user ?? "admin", pass ?? "admin"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Login fallido. Verifica credenciales o conexión.");
                    Console.ResetColor();
                    Console.WriteLine("Pulsa cualquier tecla para salir...");
                    Console.ReadKey();
                    return;
                }

                bool exit = false;
                while (!exit)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("=== PANEL DE CONTROL ===");
                    Console.ResetColor();
                    Console.WriteLine("1. Ver catálogo de Especies");
                    Console.WriteLine("2. Añadir nueva Especie");
                    Console.WriteLine("3. Editar Especie");
                    Console.WriteLine("4. Eliminar Especie");
                    Console.WriteLine("------------------------");
                    Console.WriteLine("5. Registrar nuevo Usuario");
                    Console.WriteLine("6. Salir");
                    
                    Console.Write("\n> Selecciona una opción: ");
                    var option = Console.ReadLine();

                    switch (option)
                    {
                        case "1":
                            await ListSpecies();
                            break;
                        case "2":
                            await CreateSpecies();
                            break;
                        case "3":
                            await EditSpecies();
                            break;
                        case "4":
                            await DeleteSpecies();
                            break;
                        case "5":
                            await CreateUser();
                            break;
                        case "6":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Opción no válida.");
                            break;
                    }

                    if (!exit)
                    {
                        Console.WriteLine("\nPulsa ENTER para volver al menú...");
                        Console.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR CRÍTICO: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Pulsa cualquier tecla para salir...");
                Console.ReadKey();
            }
        }

        static async Task ListSpecies()
        {
            Console.WriteLine("\n--- CATÁLOGO DE ESPECIES ---");
            var list = await _api.GetAllAsync();
            if (list.Count == 0) Console.WriteLine("No hay especies registradas.");
            
            // Cabecera ajustada
            Console.WriteLine($"{"ID",-4} | {"Nombre Científico",-25} | {"Wiki URL",-30} | {"Foto URL (trunc)"}");
            Console.WriteLine(new string('-', 100));
            
            foreach (var item in list)
            {
                var photo = item.PhotoUrl?.Length > 30 ? item.PhotoUrl.Substring(0, 27) + "..." : item.PhotoUrl ?? "N/A";
                var wiki = item.AntWikiUrl?.Length > 30 ? item.AntWikiUrl.Substring(0, 27) + "..." : item.AntWikiUrl ?? "N/A";
                Console.WriteLine($"{item.Id,-4} | {item.ScientificName,-25} | {wiki,-30} | {photo}");
            }
        }

        static async Task CreateSpecies()
        {
            Console.WriteLine("\n--- NUEVA ESPECIE ---");
            Console.Write("Nombre científico: ");
            var name = Console.ReadLine();
            Console.Write("URL AntWiki: ");
            var wiki = Console.ReadLine();
            Console.Write("URL Foto: ");
            var photo = Console.ReadLine();
            Console.Write("iNaturalist ID: ");
            var inat = Console.ReadLine();
            Console.Write("Descripción: ");
            var desc = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Nombre obligatorio."); return; }

            await _api.CreateAsync(new AntSpecies { 
                ScientificName = name, 
                AntWikiUrl = wiki,
                PhotoUrl = photo,
                InaturalistId = inat,
                Description = desc
            });
        }

        static async Task EditSpecies()
        {
            await ListSpecies();
            Console.Write("\nID de la especie a editar: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                // Obtenemos la lista para buscar el actual y mostrarlo por defecto (opcional, pero mejor UX)
                // Aquí solo pedimos datos nuevos.
                
                Console.WriteLine("(Deja en blanco para mantener el valor actual)");
                
                Console.Write("Nuevo Nombre científico: ");
                var name = Console.ReadLine();
                Console.Write("Nueva URL AntWiki: ");
                var wiki = Console.ReadLine();
                Console.Write("Nueva URL Foto: ");
                var photo = Console.ReadLine();
                Console.Write("Nuevo iNaturalist ID: ");
                var inat = Console.ReadLine();

                // Nota: Idealmente deberíamos hacer un GET(id) primero para no machacar con vacíos si la API no soporta PATCH.
                // Como nuestra API hace Update completo, necesitamos enviar el objeto entero.
                // Vamos a buscarlo en la lista local (que acabamos de cargar en ListSpecies, aunque no la guardamos).
                // Haremos una llamada extra para hacerlo bien.
                
                // Pero como ApiClient no tiene GetById público expuesto en esta versión simplificada,
                // asumiremos que el usuario rellena lo que quiere cambiar.
                // Para evitar borrar datos, voy a mejorar esto: obtener la lista, buscar localmente y fusionar.
                
                var all = await _api.GetAllAsync();
                var current = all.FirstOrDefault(x => x.Id == id);
                
                if (current == null) {
                    Console.WriteLine("ID no encontrado.");
                    return;
                }

                current.ScientificName = string.IsNullOrWhiteSpace(name) ? current.ScientificName : name;
                current.AntWikiUrl = string.IsNullOrWhiteSpace(wiki) ? current.AntWikiUrl : wiki;
                current.PhotoUrl = string.IsNullOrWhiteSpace(photo) ? current.PhotoUrl : photo;
                current.InaturalistId = string.IsNullOrWhiteSpace(inat) ? current.InaturalistId : inat;

                await _api.UpdateAsync(current);
            }
        }

        static async Task DeleteSpecies()
        {
            Console.Write("\nID a eliminar: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                Console.Write($"¿Seguro que quieres borrar la ID {id}? (s/n): ");
                if (Console.ReadLine()?.ToLower() == "s")
                    await _api.DeleteAsync(id);
            }
        }

        static async Task CreateUser()
        {
            Console.WriteLine("\n--- REGISTRAR NUEVO USUARIO ---");
            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var pass = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pass))
                await _api.RegisterAsync(email, pass);
            else
                Console.WriteLine("Datos inválidos.");
        }

        static string ReadPassword()
        {
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if(key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            return pass;
        }
    }
}