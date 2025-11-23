namespace RevloDB.Services.Interfaces
{
    public interface IAPIKeyService
    {
        Task<ApiKeyDto> CreateApiKeyAsync(int userId, CreateApiKeyDto createApiKeyDto);
        Task<IEnumerable<ApiKeyDto>> GetUserApiKeysAsync(int userId);
        Task DeleteApiKeyAsync(int userId, int apiKeyId);
    }
}