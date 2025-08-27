using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IKeyValueService
    {
        Task<KeyDto?> GetKeyAsync(string keyName, int namespaceId);
        Task<string?> GetValueAsync(string keyName, int namespaceId);
        Task<IEnumerable<KeyDto>> GetAllKeysAsync(int namespaceId);
        Task<KeyDto> CreateKeyAsync(CreateKeyDto createKeyDto, int namespaceId);
        Task<KeyDto> UpdateKeyAsync(string keyName, UpdateKeyDto updateKeyDto, int namespaceId);
        Task DeleteKeyAsync(string keyName, int namespaceId);
        Task RestoreKeyAsync(string keyName, int namespaceId);
        Task<IEnumerable<VersionDto>> GetKeyHistoryAsync(string keyName, int namespaceId);
        Task<string?> GetValueAtVersionAsync(string keyName, int versionNumber, int namespaceId);
        Task<KeyDto> RevertKeyAsync(RevertKeyDto revertKeyDto, int namespaceId);
    }
}