using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IKeyValueService
    {
        Task<KeyDto?> GetKeyAsync(string keyName);
        Task<string?> GetValueAsync(string keyName);
        Task<IEnumerable<KeyDto>> GetAllKeysAsync();
        Task<KeyDto> CreateKeyAsync(CreateKeyDto createKeyDto);
        Task<KeyDto> UpdateKeyAsync(string keyName, UpdateKeyDto updateKeyDto);
        Task DeleteKeyAsync(string keyName);

        Task RestoreKeyAsync(string keyName);
        Task<IEnumerable<VersionDto>> GetKeyHistoryAsync(string keyName);
        Task<string?> GetValueAtVersionAsync(string keyName, int versionNumber);
        Task<KeyDto> RevertKeyAsync(RevertKeyDto revertKeyDto);
    }
}