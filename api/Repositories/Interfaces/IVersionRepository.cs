using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IVersionRepository
    {
        Task<Entities.Version?> GetByIdAsync(int id);
        Task<IEnumerable<Entities.Version>> GetVersionsByKeyIdAsync(int keyId);
        Task<Entities.Version?> GetLatestVersionByKeyIdAsync(int keyId);
    }
}