using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;

namespace Persistence
{
    public class MemoryRepository : IRepository<AntSpecies>
    {
        private static List<AntSpecies> _data = new List<AntSpecies>();
        
        public async Task<IEnumerable<AntSpecies>> GetAllAsync()
        {
            return await Task.FromResult(_data.ToList());
        }

        public async Task<AntSpecies?> GetByIdAsync(int id)
        {
            var item = _data.FirstOrDefault(x => x.Id == id);
            return await Task.FromResult(item);
        }

        public async Task AddAsync(AntSpecies entity)
        {
            if (entity.Id == 0)
            {
                entity.Id = _data.Count > 0 ? _data.Max(x => x.Id) + 1 : 1;
            }
            _data.Add(entity);
            await Task.CompletedTask;
        }

        public async Task AddRangeAsync(IEnumerable<AntSpecies> entities)
        {
            int maxId = _data.Count > 0 ? _data.Max(x => x.Id) : 0;
            foreach (var item in entities)
            {
                maxId++;
                item.Id = maxId;
                _data.Add(item);
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var item = _data.FirstOrDefault(x => x.Id == id);
            if (item != null) _data.Remove(item);
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(AntSpecies entity)
        {
            var index = _data.FindIndex(x => x.Id == entity.Id);
            if (index != -1) _data[index] = entity;
            await Task.CompletedTask;
        }

        // En memoria no manejamos descripciones; devolvemos null.
        public async Task<string?> GetDescriptionAsync(int id)
        {
            return await Task.FromResult<string?>(null);
        }
    }
}