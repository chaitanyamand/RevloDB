using RevloDB.DTOs;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class KeyValueService : IKeyValueService
    {
        private readonly IKeyRepository _keyRepository;
        private readonly IVersionRepository _versionRepository;

        public KeyValueService(IKeyRepository keyRepository, IVersionRepository versionRepository)
        {
            _keyRepository = keyRepository;
            _versionRepository = versionRepository;
        }

        public async Task<KeyDto?> GetKeyAsync(string keyName, int namespaceId)
        {
            var key = await _keyRepository.GetByNameAsync(keyName, namespaceId);
            if (key == null) return null;

            return new KeyDto
            {
                Id = key.Id,
                KeyName = key.KeyName,
                CurrentValue = key.CurrentVersion?.Value,
                CurrentVersionNumber = key.CurrentVersion?.VersionNumber,
                CreatedAt = key.CreatedAt
            };
        }

        public async Task<string?> GetValueAsync(string keyName, int namespaceId)
        {
            var key = await _keyRepository.GetByNameAsync(keyName, namespaceId);
            return key?.CurrentVersion?.Value;
        }

        public async Task<IEnumerable<KeyDto>> GetAllKeysAsync(int namespaceId)
        {
            var keys = await _keyRepository.GetAllAsync(namespaceId);
            return keys.Select(k => new KeyDto
            {
                Id = k.Id,
                KeyName = k.KeyName,
                CurrentValue = k.CurrentVersion?.Value,
                CurrentVersionNumber = k.CurrentVersion?.VersionNumber,
                CreatedAt = k.CreatedAt
            });
        }

        public async Task<KeyDto> CreateKeyAsync(CreateKeyDto createKeyDto, int namespaceId)
        {
            var key = await _keyRepository.CreateKeyWithVersionAsync(
                createKeyDto.KeyName,
                createKeyDto.Value,
                namespaceId
            );

            return new KeyDto
            {
                Id = key.Id,
                KeyName = key.KeyName,
                CurrentValue = key.CurrentVersion?.Value,
                CurrentVersionNumber = key.CurrentVersion?.VersionNumber,
                CreatedAt = key.CreatedAt
            };
        }

        public async Task<KeyDto> UpdateKeyAsync(string keyName, UpdateKeyDto updateKeyDto, int namespaceId)
        {
            var updatedKey = await _keyRepository.AddNewVersionAsync(keyName, updateKeyDto.Value, namespaceId);

            return new KeyDto
            {
                Id = updatedKey.Id,
                KeyName = updatedKey.KeyName,
                CurrentValue = updatedKey.CurrentVersion?.Value,
                CurrentVersionNumber = updatedKey.CurrentVersion?.VersionNumber,
                CreatedAt = updatedKey.CreatedAt
            };
        }

        public async Task DeleteKeyAsync(string keyName, int namespaceId)
        {
            var deleted = await _keyRepository.DeleteByNameAsync(keyName, namespaceId);
            if (!deleted)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found");
            }
        }

        public async Task RestoreKeyAsync(string keyName, int namespaceId)
        {
            var restored = await _keyRepository.RestoreByNameAsync(keyName, namespaceId);
            if (!restored)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found or is not deleted");
            }
        }

        public async Task<IEnumerable<VersionDto>> GetKeyHistoryAsync(string keyName, int namespaceId)
        {
            var key = await _keyRepository.GetByNameAsync(keyName, namespaceId);
            if (key == null)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found");
            }

            var versions = await _versionRepository.GetVersionsByKeyIdAsync(key.Id);
            return versions.Select(v => new VersionDto
            {
                Id = v.Id,
                Value = v.Value,
                Timestamp = v.Timestamp,
                VersionNumber = v.VersionNumber,
                KeyId = v.KeyId
            });
        }

        public async Task<string?> GetValueAtVersionAsync(string keyName, int versionNumber, int namespaceId)
        {
            var key = await _keyRepository.GetByNameAsync(keyName, namespaceId);
            if (key == null) return null;

            var versions = await _versionRepository.GetVersionsByKeyIdAsync(key.Id);
            var version = versions.FirstOrDefault(v => v.VersionNumber == versionNumber);

            return version?.Value;
        }

        public async Task<KeyDto> RevertKeyAsync(RevertKeyDto revertKeyDto, int namespaceId)
        {
            var revertedKey = await _keyRepository.RevertToVersionAsync(revertKeyDto.KeyName, revertKeyDto.VersionNumber!.Value, namespaceId);

            return new KeyDto
            {
                Id = revertedKey.Id,
                KeyName = revertedKey.KeyName,
                CurrentValue = revertedKey.CurrentVersion?.Value,
                CurrentVersionNumber = revertedKey.CurrentVersion?.VersionNumber,
                CreatedAt = revertedKey.CreatedAt
            };
        }
    }
}