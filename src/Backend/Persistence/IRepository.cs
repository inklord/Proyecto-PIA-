using System.Collections.Generic;
using System.Threading.Tasks;

namespace Persistence
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task DeleteAsync(int id);
        Task UpdateAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        // Extra: descripción asociada (si existe). Implementado solo para AntSpecies.
        Task<string?> GetDescriptionAsync(int id);
    }
}