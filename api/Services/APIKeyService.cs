using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using RevloDB.Entities;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class APIKeyService : IAPIKeyService
    {
        private readonly IUserRepository _userRepository;
        private readonly INamespaceRepository _namespaceRepository;
        private readonly IApiKeyRepository _apiKeyRepository;

        public APIKeyService(
            IUserRepository userRepository,
            INamespaceRepository namespaceRepository,
            IApiKeyRepository apiKeyRepository)
        {
            _userRepository = userRepository;
            _namespaceRepository = namespaceRepository;
            _apiKeyRepository = apiKeyRepository;
        }

        public async Task<ApiKeyDto> CreateApiKeyAsync(int userId, CreateApiKeyDto createApiKeyDto, int namespaceId)
        {
            // Verify user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                throw new InvalidOperationException("User not found");
            }

            // Verify namespace exists and user has permission
            var userNamespace = await _userRepository.GetUserNamespaceAsync(userId, namespaceId);
            if (userNamespace == null)
            {
                throw new UnauthorizedAccessException("You don't have access to this namespace");
            }

            // Validate role (assuming roles like "read", "write", "admin")
            var validRoles = new[] { "read", "write", "admin" };
            if (!validRoles.Contains(createApiKeyDto.Role.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");
            }

            var apiKey = new ApiKey
            {
                UserId = userId,
                NamespaceId = namespaceId,
                KeyValue = GenerateApiKey(),
                Role = createApiKeyDto.Role.ToLowerInvariant(),
                Description = createApiKeyDto.Description,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = createApiKeyDto.ExpiresAt,
                IsDeleted = false
            };

            var createdApiKey = await _apiKeyRepository.CreateAsync(apiKey);
            var namespaceInfo = await _namespaceRepository.GetByIdAsync(namespaceId);

            return new ApiKeyDto
            {
                Id = createdApiKey.Id,
                KeyValue = createdApiKey.KeyValue,
                Role = createdApiKey.Role,
                Description = createdApiKey.Description,
                NamespaceId = createdApiKey.NamespaceId,
                NamespaceName = namespaceInfo?.Name ?? "",
                CreatedAt = createdApiKey.CreatedAt,
                ExpiresAt = createdApiKey.ExpiresAt
            };
        }

        public async Task<IEnumerable<ApiKeyDto>> GetUserApiKeysAsync(int userId)
        {
            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId);
            var result = new List<ApiKeyDto>();

            foreach (var apiKey in apiKeys.Where(ak => !ak.IsDeleted))
            {
                var namespaceInfo = await _namespaceRepository.GetByIdAsync(apiKey.NamespaceId);
                result.Add(new ApiKeyDto
                {
                    Id = apiKey.Id,
                    KeyValue = MaskApiKey(apiKey.KeyValue),
                    Role = apiKey.Role,
                    Description = apiKey.Description,
                    NamespaceId = apiKey.NamespaceId,
                    NamespaceName = namespaceInfo?.Name ?? "",
                    CreatedAt = apiKey.CreatedAt,
                    ExpiresAt = apiKey.ExpiresAt
                });
            }

            return result;
        }

        public async Task DeleteApiKeyAsync(int userId, int apiKeyId)
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId);

            if (apiKey == null || apiKey.IsDeleted)
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

    }
}