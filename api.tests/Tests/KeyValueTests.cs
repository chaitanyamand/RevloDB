using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Utilities;
using RevloDB.DTOs;
using RevloDB.API.Tests.Setup;
using RevloDB.API.Tests.DTOs;

namespace RevloDB.API.Tests
{
    public class KeyValueControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly TestUserUtility _userUtility;
        private readonly ApiTestAppFactory _factory;

        public KeyValueControllerTests(ApiTestAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _userUtility = new TestUserUtility(_client);
        }

        public static IEnumerable<object[]> GetWriteAccessUsers()
        {
            yield return new object[] { "editor" };
            yield return new object[] { "admin" };
        }

        public static IEnumerable<object[]> GetReadAccessUsers()
        {
            yield return new object[] { "readonly" };
            yield return new object[] { "editor" };
            yield return new object[] { "admin" };
        }

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task CreateKey_WithWriteAccess_ShouldSucceed(string userRole)
        {
            var user = await _userUtility.GetUserAsync(userRole);
            var keyName = $"new-key-{Guid.NewGuid().ToString("N")[..8]}";
            var createDto = new CreateKeyDto { KeyName = keyName, Value = "initial-value" };

            var response = await CreateTestKeyAsync(user, createDto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdKey = await response.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(createdKey);
            Assert.Equal(keyName, createdKey.KeyName);
        }

        [Fact]
        public async Task CreateKey_WithReadonlyUser_ShouldBeForbidden()
        {
            var user = await _userUtility.GetReadonlyUserAsync();
            var keyName = $"forbidden-key-{Guid.NewGuid().ToString("N")[..8]}";
            var createDto = new CreateKeyDto { KeyName = keyName, Value = "some-value" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={user.NamespaceId}", createDto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetReadAccessUsers))]
        public async Task GetKey_WithReadAccess_ShouldReturnKey(string userRole)
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var keyName = $"test-key-for-get-{Guid.NewGuid().ToString("N")[..8]}";
            await CreateTestKeyAsync(admin, new CreateKeyDto { KeyName = keyName, Value = "test-value" });

            var user = await _userUtility.GetUserAsync(userRole);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={user.NamespaceId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var keyDto = await response.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(keyDto);
            Assert.Equal(keyName, keyDto.KeyName);
        }

        [Fact]
        public async Task GetKey_FromAnotherNamespace_ShouldReturnForbidden()
        {
            var userA = await _userUtility.GetNamespaceOwnerAsync();
            var keyName = $"user-a-secret-key-{Guid.NewGuid().ToString("N")[..8]}";
            await CreateTestKeyAsync(userA, new CreateKeyDto { KeyName = keyName, Value = "secret" });

            var userBUtility = new TestUserUtility(_client);
            var userB = await userBUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userB.AccessToken);
            var response = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={userB.NamespaceId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task UpdateKey_WithWriteAccess_ShouldSucceed(string userRole)
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var keyName = $"key-to-update-{Guid.NewGuid().ToString("N")[..8]}";
            await CreateTestKeyAsync(admin, new CreateKeyDto { KeyName = keyName, Value = "version1" });

            var user = await _userUtility.GetUserAsync(userRole);
            var updateDto = new UpdateKeyDto { Value = "version2" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={user.NamespaceId}", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task FullWorkflow_CreateUpdateRevertHistory_ShouldWorkCorrectly()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var keyName = $"workflow-key-{Guid.NewGuid().ToString("N")[..8]}";
            var namespaceId = admin.NamespaceId;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

            await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={namespaceId}", new CreateKeyDto { KeyName = keyName, Value = "Value-V1" });

            await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}", new UpdateKeyDto { Value = "Value-V2" });
            await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}", new UpdateKeyDto { Value = "Value-V3" });

            var historyResponse = await _client.GetAsync($"/api/v1/keyvalue/{keyName}/history?namespaceId={namespaceId}");
            historyResponse.EnsureSuccessStatusCode();
            var versions = await historyResponse.Content.ReadFromJsonAsync<List<VersionDto>>();
            Assert.NotNull(versions);
            Assert.Equal(3, versions.Count);

            var revertDto = new RevertKeyDto { KeyName = keyName, VersionNumber = 1 };
            var revertResponse = await _client.PostAsJsonAsync($"/api/v1/keyvalue/revert?namespaceId={namespaceId}", revertDto);
            revertResponse.EnsureSuccessStatusCode();

            var finalGetResponse = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}");
            finalGetResponse.EnsureSuccessStatusCode();
            var finalKey = await finalGetResponse.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(finalKey);
            Assert.Equal("Value-V1", finalKey.CurrentValue);
            Assert.Equal(1, finalKey.CurrentVersionNumber);
        }

        private async Task<HttpResponseMessage> CreateTestKeyAsync(AuthenticatedUser user, CreateKeyDto dto)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            return await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={user.NamespaceId}", dto);
        }
    }
}