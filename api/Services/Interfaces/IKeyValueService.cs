using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IKeyValueService
    {
        Task<KeyValueDto?> GetKeyAsync(string branchName, string keyName, int namespaceId);
        Task<KeyValueListDto> GetAllKeysAsync(string branchName, int namespaceId);
        Task SetKeyAsync(string branchName, string keyName, string value, int namespaceId);
        Task DeleteKeyAsync(string branchName, string keyName, int namespaceId);
        Task<List<KeyValueDto>> GetUnstagedChangesAsync(string branchName, int namespaceId);
    }
}
