using RevloDB.DTOs;
using RevloDB.Entities;
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

        public async Task<KeyDto?> GetKeyAsync(string keyName)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
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

        public async Task<string?> GetValueAsync(string keyName)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
            return key?.CurrentVersion?.Value;
        }

        public async Task<IEnumerable<KeyDto>> GetAllKeysAsync()
        {
            var keys = await _keyRepository.GetAllAsync();
            return keys.Select(k => new KeyDto
            {
                Id = k.Id,
                KeyName = k.KeyName,
                CurrentValue = k.CurrentVersion?.Value,
                CurrentVersionNumber = k.CurrentVersion?.VersionNumber,
                CreatedAt = k.CreatedAt
            });
        }

        public async Task<KeyDto> CreateKeyAsync(CreateKeyDto createKeyDto)
        {
            var key = await _keyRepository.CreateKeyWithVersionAsync(
                createKeyDto.KeyName,
                createKeyDto.Value
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

        public async Task<KeyDto> UpdateKeyAsync(string keyName, UpdateKeyDto updateKeyDto)
        {
            var updatedKey = await _keyRepository.AddNewVersionAsync(keyName, updateKeyDto.Value);

            return new KeyDto
            {
                Id = updatedKey.Id,
                KeyName = updatedKey.KeyName,
                CurrentValue = updatedKey.CurrentVersion?.Value,
                CurrentVersionNumber = updatedKey.CurrentVersion?.VersionNumber,
                CreatedAt = updatedKey.CreatedAt
            };
        }

        public async Task DeleteKeyAsync(string keyName)
        {
            var deleted = await _keyRepository.DeleteByNameAsync(keyName);
            if (!deleted)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found");
            }
        }

        public async Task RestoreKeyAsync(string keyName)
        {
            var restored = await _keyRepository.RestoreByNameAsync(keyName);
            if (!restored)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found or is not deleted");
            }
        }

        public async Task<IEnumerable<VersionDto>> GetKeyHistoryAsync(string keyName)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
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

        public async Task<string?> GetValueAtVersionAsync(string keyName, int versionNumber)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
            if (key == null) return null;

            var versions = await _versionRepository.GetVersionsByKeyIdAsync(key.Id);
            var version = versions.FirstOrDefault(v => v.VersionNumber == versionNumber);

            return version?.Value;
        }

        public async Task<KeyDto> RevertKeyAsync(RevertKeyDto revertKeyDto)
        {
            var revertedKey = await _keyRepository.RevertToVersionAsync(revertKeyDto.KeyName, revertKeyDto.VersionNumber);

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