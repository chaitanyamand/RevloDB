using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IVersionRepository
    {
        Task<Entities.Version?> GetByIdAsync(int id);
        Task<IEnumerable<Entities.Version>> GetVersionsByKeyIdAsync(int keyId);
        Task<Entities.Version?> GetLatestVersionByKeyIdAsync(int keyId);
        Task<Entities.Version> CreateAsync(Entities.Version version);
        Task<int> GetNextVersionNumberAsync(int keyId);
    }
}