using System.Security.Cryptography;
using RevloDB.Entities;
using RevloDB.Extensions;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Services
{
    public class APIKeyService : IAPIKeyService
    {
        private readonly IUserRepository _userRepository;
        private readonly INamespaceRepository _namespaceRepository;
        private readonly IAPIKeyRepository _apiKeyRepository;
        private readonly IUserNamespaceRepository _userNamespaceRepository;

        public APIKeyService(
            IUserRepository userRepository,
            INamespaceRepository namespaceRepository,
            IAPIKeyRepository apiKeyRepository,
            IUserNamespaceRepository userNamespaceRepository)
        {
            _userRepository = userRepository;
            _namespaceRepository = namespaceRepository;
            _apiKeyRepository = apiKeyRepository;
            _userNamespaceRepository = userNamespaceRepository;
        }

        public async Task<ApiKeyDto> CreateApiKeyAsync(int userId, CreateApiKeyDto createApiKeyDto)
        {
            var namespaceId = createApiKeyDto.NamespaceId;
            var roleForAPIKey = createApiKeyDto.Role.ToEnumOrThrow<NamespaceRole>("Invalid role specified for API key");
            (bool isValidExpiry, string? expiryError) = ValidateExpiryTime(createApiKeyDto.ExpiresAtInDays);
            if (!isValidExpiry)
            {
                throw new ArgumentException(expiryError);
            }
            var apiKeyExpiresAt = createApiKeyDto.ExpiresAtInDays.HasValue
                ? DateTime.UtcNow.AddDays(createApiKeyDto.ExpiresAtInDays.Value)
                : DateTime.UtcNow.AddDays(14);
            var apiKeyDescription = createApiKeyDto.Description;

            var userNamespaceEntry = await _userNamespaceRepository.GetUserNamespaceEntryAsync(userId, namespaceId);

            if (userNamespaceEntry == null)
            {
                throw new UnauthorizedAccessException("You don't have access to this namespace");
            }

            var hasSufficientRole = RoleCheckUtil.HasSufficientRole(userNamespaceEntry.Role, roleForAPIKey);
            if (!hasSufficientRole)
            {
                throw new UnauthorizedAccessException("You don't have sufficient role to create an API key with the requested role");
            }

            var apiKey = new ApiKey
            {
                UserId = userId,
                NamespaceId = namespaceId,
                KeyValue = GenerateApiKey(),
                Role = roleForAPIKey,
                Description = apiKeyDescription,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = apiKeyExpiresAt,
                IsDeleted = false
            };

            var createdApiKey = await _apiKeyRepository.CreateAsync(apiKey);
            return new ApiKeyDto
            {
                Id = createdApiKey.Id,
                KeyValue = createdApiKey.KeyValue,
                Role = createdApiKey.Role.ToString(),
                Description = createdApiKey.Description,
                NamespaceId = createdApiKey.NamespaceId,
                NamespaceName = userNamespaceEntry.Namespace.Name,
                CreatedAt = createdApiKey.CreatedAt,
                ExpiresAt = createdApiKey.ExpiresAt
            };
        }
        public async Task<IEnumerable<ApiKeyDto>> GetUserApiKeysAsync(int userId)
        {
            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId);
            var activeApiKeys = apiKeys.Where(ak => !ak.IsDeleted && ak.ExpiresAt > DateTime.UtcNow);
            var result = new List<ApiKeyDto>();

            foreach (var apiKey in activeApiKeys)
            {
                result.Add(new ApiKeyDto
                {
                    Id = apiKey.Id,
                    KeyValue = MaskApiKey(apiKey.KeyValue),
                    Role = apiKey.Role.ToString(),
                    Description = apiKey.Description,
                    NamespaceId = apiKey.NamespaceId,
                    NamespaceName = apiKey.Namespace.Name ?? "",
                    CreatedAt = apiKey.CreatedAt,
                    ExpiresAt = apiKey.ExpiresAt
                });
            }

            return result;
        }

        public async Task DeleteApiKeyAsync(int userId, int apiKeyId)
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId);

            if (apiKey == null)
            {
                throw new InvalidOperationException("API key not found");
            }

            if (apiKey.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only delete your own API keys");
            }

            await _apiKeyRepository.DeleteAsync(apiKeyId);
        }

        private string GenerateApiKey()
        {
            const int keyLength = 64;
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[keyLength];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }

        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
                return apiKey;

            return apiKey.Substring(0, 4) + new string('*', apiKey.Length - 8) + apiKey.Substring(apiKey.Length - 4);
        }

        private (bool IsValid, string? Error) ValidateExpiryTime(int? expiresAtInDays)
        {
            if (expiresAtInDays.HasValue && expiresAtInDays <= 0)
            {
                return (false, "Expiry time must be a positive integer");
            }
            else if (expiresAtInDays.HasValue && expiresAtInDays > 30)
            {
                return (false, "Expiry time cannot exceed 30 days");
            }
            return (true, null);
        }

    }
}