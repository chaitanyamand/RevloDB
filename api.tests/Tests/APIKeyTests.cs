using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Utilities;
using RevloDB.DTOs;
using RevloDB.API.Tests.Setup;
using RevloDB.API.Tests.DTOs;

namespace RevloDB.API.Tests
{
    public class APIKeyControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly TestUserUtility _userUtility;
        private readonly ApiTestAppFactory _factory;

        public APIKeyControllerTests(ApiTestAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _userUtility = new TestUserUtility(_client);
        }

        #region CreateApiKey Tests

        [Fact]
        public async Task CreateApiKey_WithValidData_ShouldReturnCreated()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "ReadOnly",
                Description = "Test API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var apiKey = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
            Assert.NotNull(apiKey);
            Assert.Equal("ReadOnly", apiKey.Role);
            Assert.Equal("Test API key", apiKey.Description);
            Assert.Equal(user.NamespaceId, apiKey.NamespaceId);
            Assert.NotEmpty(apiKey.KeyValue);
        }

        [Fact]
        public async Task CreateApiKey_WithExpirationDays_ShouldSetExpirationDate()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "Editor",
                Description = "Expiring API key",
                ExpiresAtInDays = 30
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var apiKey = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
            Assert.NotNull(apiKey);
            Assert.NotNull(apiKey.ExpiresAt);
            Assert.True(apiKey.ExpiresAt > DateTime.UtcNow.AddDays(29));
            Assert.True(apiKey.ExpiresAt < DateTime.UtcNow.AddDays(31));
        }

        [Fact]
        public async Task CreateApiKey_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = 1,
                Role = "ReadOnly",
                Description = "Test API key"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateApiKey_WithInvalidToken_ShouldReturnUnauthorized()
        {
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = 1,
                Role = "ReadOnly",
                Description = "Test API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateApiKey_WithMissingNamespaceId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                Role = "ReadOnly",
                Description = "Test API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateApiKey_WithMissingRole_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Description = "Test API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateApiKey_WithTooLongRole_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = new string('a', 51),
                Description = "Test API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateApiKey_WithTooLongDescription_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "ReadOnly",
                Description = new string('a', 256)
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region GetApiKeys Tests

        [Fact]
        public async Task GetApiKeys_WithAuthentication_ShouldReturnUserApiKeys()
        {
            var user = await _userUtility.GetAdminUserAsync();

            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "ReadOnly",
                Description = "Test API key for get test"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);

            var response = await _client.GetAsync("/api/v1/user/api-keys");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var apiKeys = await response.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(apiKeys);
            Assert.NotEmpty(apiKeys);

            var testKey = apiKeys.FirstOrDefault(k => k.Description == "Test API key for get test");
            Assert.NotNull(testKey);
            Assert.Equal("ReadOnly", testKey.Role);
        }

        [Fact]
        public async Task GetApiKeys_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/user/api-keys");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetApiKeys_WithInvalidToken_ShouldReturnUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.GetAsync("/api/v1/user/api-keys");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetApiKeys_ForNewUser_ShouldReturnEmptyList()
        {
            var user = await _userUtility.GetEditorUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/user/api-keys");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var apiKeys = await response.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(apiKeys);
            Assert.Empty(apiKeys);
        }

        [Fact]
        public async Task GetApiKeys_ShouldOnlyReturnCurrentUserKeys()
        {
            var user1 = await _userUtility.GetAdminUserAsync();
            var user2 = await _userUtility.GetEditorUserAsync();

            var createDto1 = new CreateApiKeyDto
            {
                NamespaceId = user1.NamespaceId,
                Role = "ReadOnly",
                Description = "User1 API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
            await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto1);

            var createDto2 = new CreateApiKeyDto
            {
                NamespaceId = user2.NamespaceId,
                Role = "Editor",
                Description = "User2 API key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
            await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto2);

            var response = await _client.GetAsync("/api/v1/user/api-keys");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var apiKeys = await response.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(apiKeys);
            Assert.Single(apiKeys);
            Assert.Equal("User2 API key", apiKeys.First().Description);
            Assert.Equal("Editor", apiKeys.First().Role);
        }

        #endregion

        #region DeleteApiKey Tests

        [Fact]
        public async Task DeleteApiKey_WithValidId_ShouldReturnNoContent()
        {
            var user = await _userUtility.GetAdminUserAsync();

            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "ReadOnly",
                Description = "API key to delete"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);
            var createdKey = await createResponse.Content.ReadFromJsonAsync<ApiKeyDto>();

            var deleteResponse = await _client.DeleteAsync($"/api/v1/user/api-key/{createdKey!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteApiKey_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.DeleteAsync("/api/v1/user/api-key/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteApiKey_WithInvalidToken_ShouldReturnUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.DeleteAsync("/api/v1/user/api-key/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteApiKey_AfterDeletion_ShouldNotAppearInGetApiKeys()
        {
            var user = await _userUtility.GetAdminUserAsync();

            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "ReadOnly",
                Description = "API key to delete and verify"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);
            var createdKey = await createResponse.Content.ReadFromJsonAsync<ApiKeyDto>();

            var getResponse1 = await _client.GetAsync("/api/v1/user/api-keys");
            var apiKeys1 = await getResponse1.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.Contains(apiKeys1!, k => k.Id == createdKey!.Id);

            var deleteResponse = await _client.DeleteAsync($"/api/v1/user/api-key/{createdKey!.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse2 = await _client.GetAsync("/api/v1/user/api-keys");
            var apiKeys2 = await getResponse2.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.DoesNotContain(apiKeys2!, k => k.Id == createdKey.Id);
        }

        [Fact]
        public async Task DeleteApiKey_TryingToDeleteAnotherUsersKey_ShouldReturnUnauthorized()
        {
            var user1 = await _userUtility.GetAdminUserAsync();
            var user2 = await _userUtility.GetEditorUserAsync();

            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user1.NamespaceId,
                Role = "ReadOnly",
                Description = "User1's protected key"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);
            var createdKey = await createResponse.Content.ReadFromJsonAsync<ApiKeyDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
            var deleteResponse = await _client.DeleteAsync($"/api/v1/user/api-key/{createdKey!.Id}");

            Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
            var getResponse = await _client.GetAsync("/api/v1/user/api-keys");
            var apiKeys = await getResponse.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.Contains(apiKeys!, k => k.Id == createdKey.Id);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task APIKeyLifecycle_CreateGetDelete_ShouldWorkCorrectly()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var keyDescription = $"Lifecycle test key {Guid.NewGuid().ToString("N")[..8]}";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

            var createDto = new CreateApiKeyDto
            {
                NamespaceId = user.NamespaceId,
                Role = "Editor",
                Description = keyDescription,
                ExpiresAtInDays = 7
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createdKey = await createResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
            Assert.NotNull(createdKey);
            Assert.Equal(keyDescription, createdKey.Description);
            Assert.NotEmpty(createdKey.KeyValue);

            var getResponse = await _client.GetAsync("/api/v1/user/api-keys");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var apiKeys = await getResponse.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(apiKeys);
            var foundKey = apiKeys.FirstOrDefault(k => k.Description == keyDescription);
            Assert.NotNull(foundKey);
            Assert.Equal("Editor", foundKey.Role);
            Assert.Equal(user.NamespaceId, foundKey.NamespaceId);

            var deleteResponse = await _client.DeleteAsync($"/api/v1/user/api-key/{createdKey.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var finalGetResponse = await _client.GetAsync("/api/v1/user/api-keys");
            Assert.Equal(HttpStatusCode.OK, finalGetResponse.StatusCode);
            var finalApiKeys = await finalGetResponse.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(finalApiKeys);
            Assert.DoesNotContain(finalApiKeys, k => k.Description == keyDescription);
        }

        [Fact]
        public async Task CreateMultipleApiKeys_WithDifferentRoles_ShouldAllBeRetrievable()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var keyPrefix = $"multi-test-{Guid.NewGuid().ToString("N")[..8]}";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

            var roles = new[] { "ReadOnly", "Editor", "Admin" };
            var createdKeys = new List<ApiKeyDto>();

            foreach (var role in roles)
            {
                var createDto = new CreateApiKeyDto
                {
                    NamespaceId = user.NamespaceId,
                    Role = role,
                    Description = $"{keyPrefix}-{role}"
                };

                var response = await _client.PostAsJsonAsync("/api/v1/user/api-key", createDto);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var apiKey = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
                createdKeys.Add(apiKey!);
            }

            var getResponse = await _client.GetAsync("/api/v1/user/api-keys");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var apiKeys = await getResponse.Content.ReadFromJsonAsync<IEnumerable<ApiKeyDto>>();
            Assert.NotNull(apiKeys);

            foreach (var role in roles)
            {
                var foundKey = apiKeys.FirstOrDefault(k => k.Description == $"{keyPrefix}-{role}");
                Assert.NotNull(foundKey);
                Assert.Equal(role, foundKey.Role);
            }

            foreach (var key in createdKeys)
            {
                await _client.DeleteAsync($"/api/v1/user/api-key/{key.Id}");
            }
        }

        #endregion
    }
}