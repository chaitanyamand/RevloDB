namespace RevloDB.Repositories.Interfaces
{
    public interface ICleanupRepository
    {
        Task<int> DeleteMarkedKeysAsync();
        Task<int> GetMarkedKeysCountAsync();
    }
}