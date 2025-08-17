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
            if (await _keyRepository.ExistsAsync(createKeyDto.KeyName))
            {
                throw new InvalidOperationException($"Key '{createKeyDto.KeyName}' already exists");
            }

            var key = new Key
            {
                KeyName = createKeyDto.KeyName,
                CreatedAt = DateTime.UtcNow
            };

            key = await _keyRepository.CreateAsync(key);

            var version = new Entities.Version
            {
                KeyId = key.Id,
                Value = createKeyDto.Value,
                VersionNumber = 1,
                Timestamp = DateTime.UtcNow
            };

            version = await _versionRepository.CreateAsync(version);

            key.CurrentVersionId = version.Id;
            await _keyRepository.UpdateAsync(key);

            return new KeyDto
            {
                Id = key.Id,
                KeyName = key.KeyName,
                CurrentValue = version.Value,
                CurrentVersionNumber = version.VersionNumber,
                CreatedAt = key.CreatedAt
            };
        }

        public async Task<KeyDto> UpdateKeyAsync(string keyName, UpdateKeyDto updateKeyDto)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
            if (key == null)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found");
            }

            var nextVersionNumber = await _versionRepository.GetNextVersionNumberAsync(key.Id);
            var newVersion = new Entities.Version
            {
                KeyId = key.Id,
                Value = updateKeyDto.Value,
                VersionNumber = nextVersionNumber,
                Timestamp = DateTime.UtcNow
            };

            newVersion = await _versionRepository.CreateAsync(newVersion);

            key.CurrentVersionId = newVersion.Id;
            await _keyRepository.UpdateAsync(key);

            return new KeyDto
            {
                Id = key.Id,
                KeyName = key.KeyName,
                CurrentValue = newVersion.Value,
                CurrentVersionNumber = newVersion.VersionNumber,
                CreatedAt = key.CreatedAt
            };
        }

        public async Task DeleteKeyAsync(string keyName)
        {
            var key = await _keyRepository.GetByNameAsync(keyName);
            if (key == null)
            {
                throw new KeyNotFoundException($"Key '{keyName}' not found");
            }

            await _keyRepository.DeleteAsync(key.Id);
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
    }

    public class KeyNotFoundException : Exception
    {
        public KeyNotFoundException(string message) : base(message) { }
    }
}