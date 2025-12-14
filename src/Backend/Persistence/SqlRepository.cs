using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Models;
using MySql.Data.MySqlClient;

namespace Persistence
{
    public class SqlRepository : IRepository<AntSpecies>
    {
        private readonly string _connectionString;

        public SqlRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<IEnumerable<AntSpecies>> GetAllAsync()
        {
            var list = new List<AntSpecies>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM species", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(MapReader(reader));
                    }
                }
            }
            return list;
        }

        public async Task<AntSpecies?> GetByIdAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM species WHERE id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapReader(reader);
                    }
                }
            }
            return null;
        }

        public async Task AddAsync(AntSpecies entity)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                // Usamos IGNORE o control de errores para depurar
                // Asumimos que created_at y updated_at tienen valores por defecto o son opcionales.
                // Si 'region' es obligatorio y no tiene default, fallará. Probamos enviando NULL explícito.
                var sql = "INSERT INTO species (scientific_name, antwiki_url, photo_url, inaturalist_id, region, created_at, updated_at) VALUES (@Name, @Wiki, @Photo, @Inat, NULL, NOW(), NOW())";
                var cmd = new MySqlCommand(sql, conn);
                BindParams(cmd, entity);
                
                try 
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    // Relanzar con mensaje claro para que la Consola lo muestre
                    throw new Exception($"Error DB: {ex.Message} (Code: {ex.Number})");
                }

                if (!string.IsNullOrWhiteSpace(entity.Description))
                {
                    var sqlDesc = "INSERT INTO species_descriptions (scientific_name, description, last_updated, source) VALUES (@Name, @Desc, NOW(), 'manual')";
                    var cmdDesc = new MySqlCommand(sqlDesc, conn);
                    cmdDesc.Parameters.AddWithValue("@Name", entity.ScientificName);
                    cmdDesc.Parameters.AddWithValue("@Desc", entity.Description);
                    try { await cmdDesc.ExecuteNonQueryAsync(); } catch { }
                }
            }
        }

        public async Task AddRangeAsync(IEnumerable<AntSpecies> entities)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var trans = await conn.BeginTransactionAsync())
                {
                    try 
                    {
                        foreach(var entity in entities)
                        {
                            var sql = "INSERT INTO species (scientific_name, antwiki_url, photo_url, inaturalist_id) VALUES (@Name, @Wiki, @Photo, @Inat)";
                            var cmd = new MySqlCommand(sql, conn);
                            BindParams(cmd, entity);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await trans.CommitAsync();
                    }
                    catch
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task DeleteAsync(int id)
        {
             using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                // Ojo: Si hay FKs, borrar species podría fallar si no se borra description antes.
                // Asumimos ON DELETE CASCADE en la BD o borramos descripción primero.
                // Para simplificar, intentamos borrar species directo.
                var cmd = new MySqlCommand("DELETE FROM species WHERE id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateAsync(AntSpecies entity)
        {
             using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var sql = "UPDATE species SET scientific_name=@Name, antwiki_url=@Wiki, photo_url=@Photo, inaturalist_id=@Inat WHERE id=@Id";
                var cmd = new MySqlCommand(sql, conn);
                BindParams(cmd, entity);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                await cmd.ExecuteNonQueryAsync();

                if (!string.IsNullOrWhiteSpace(entity.Description))
                {
                    // Update o Insert description
                    var sqlDesc = @"INSERT INTO species_descriptions (scientific_name, description) VALUES (@Name, @Desc) 
                                    ON DUPLICATE KEY UPDATE description=@Desc";
                    var cmdDesc = new MySqlCommand(sqlDesc, conn);
                    cmdDesc.Parameters.AddWithValue("@Name", entity.ScientificName);
                    cmdDesc.Parameters.AddWithValue("@Desc", entity.Description);
                    try { await cmdDesc.ExecuteNonQueryAsync(); } catch { }
                }
            }
        }

        public async Task<string?> GetDescriptionAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var sql = @"
                    SELECT sd.description
                    FROM species_descriptions sd
                    JOIN species s ON sd.scientific_name = s.scientific_name
                    WHERE s.id = @Id
                    LIMIT 1";

                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? null : result.ToString();
            }
        }

        private void BindParams(MySqlCommand cmd, AntSpecies entity)
        {
            cmd.Parameters.AddWithValue("@Name", entity.ScientificName);
            // Mapeo nombres C# -> nombres DB
            // antwiki_url
            // photo_url
            // inaturalist_id
            cmd.Parameters.AddWithValue("@Wiki", entity.AntWikiUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Photo", entity.PhotoUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Inat", entity.InaturalistId ?? (object)DBNull.Value);
        }

        private AntSpecies MapReader(System.Data.Common.DbDataReader reader)
        {
            // OJO: Nombres de columna en minúscula según tu captura
            return new AntSpecies
            {
                Id = Convert.ToInt32(reader["id"]),
                ScientificName = reader["scientific_name"].ToString() ?? "Desconocida",
                AntWikiUrl = HasCol(reader, "antwiki_url") ? reader["antwiki_url"] as string : null,
                PhotoUrl = HasCol(reader, "photo_url") ? reader["photo_url"] as string : null,
                InaturalistId = HasCol(reader, "inaturalist_id") ? reader["inaturalist_id"]?.ToString() : null, // A veces es int, convertimos a string
                Description = null // Se carga aparte con GetDescriptionAsync o habría que hacer JOIN
            };
        }

        private bool HasCol(System.Data.Common.DbDataReader reader, string colName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}