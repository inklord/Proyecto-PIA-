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
                // Traemos todas las especies de la tabla
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
                var sql = "INSERT INTO species (scientific_name, antwiki_url, photo_url, inaturalist_id) VALUES (@Name, @Wiki, @Photo, @Inat)";
                var cmd = new MySqlCommand(sql, conn);
                BindParams(cmd, entity);
                await cmd.ExecuteNonQueryAsync();
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
            }
        }

        public async Task<string?> GetDescriptionAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                // En tu BBDD la tabla species_descriptions está ligada por scientific_name,
                // así que buscamos la descripción usando el nombre científico de la especie.
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
            cmd.Parameters.AddWithValue("@Wiki", entity.AntWikiUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Photo", entity.PhotoUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Inat", entity.InaturalistId ?? (object)DBNull.Value);
        }

        private AntSpecies MapReader(System.Data.Common.DbDataReader reader)
        {
            return new AntSpecies
            {
                Id = Convert.ToInt32(reader["id"]),
                ScientificName = reader["scientific_name"].ToString() ?? "Desconocida",
                AntWikiUrl = reader["antwiki_url"] as string,
                PhotoUrl = reader["photo_url"] as string,
                InaturalistId = reader["inaturalist_id"] as string
            };
        }
    }
}