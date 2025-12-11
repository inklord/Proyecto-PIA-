using System.Configuration;
using Models;

namespace Persistence
{
    public static class RepositoryFactory
    {
        private static IRepository<AntSpecies> _currentRepository = new MemoryRepository();
        public static string CurrentConnectionString { get; private set; } = string.Empty;

        public static IRepository<AntSpecies> GetRepository()
        {
            return _currentRepository;
        }

        public static void Initialize(string persistenceType, string connectionString)
        {
            CurrentConnectionString = connectionString;
            if (persistenceType.ToLower() == "mysql")
            {
                _currentRepository = new SqlRepository(connectionString);
            }
            else
            {
                _currentRepository = new MemoryRepository();
            }
        }
    }
}