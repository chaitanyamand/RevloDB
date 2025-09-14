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

        #region Test Data Providers

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

        #endregion

        #region POST /api/v1/keyvalue

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task CreateKey_WithWriteAccess_ShouldSucceed(string userRole)
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var user = GetUserByRole(userRole);
            var createDto = new CreateKeyDto { KeyName = "new-key", Value = "initial-value" };

            // ACT
            var response = await CreateTestKeyAsync(user, createDto);

            // ASSERT
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdKey = await response.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(createdKey);
            Assert.Equal("new-key", createdKey.KeyName);
        }

        [Fact]
        public async Task CreateKey_WithReadonlyUser_ShouldBeForbidden()
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var user = _userUtility.GetReadonlyUsers().First();
            var createDto = new CreateKeyDto { KeyName = "forbidden-key", Value = "some-value" };

            // ACT
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            // CHANGED: Added namespaceId query parameter
            var response = await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={user.NamespaceId}", createDto);

            // ASSERT
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        #endregion

        #region GET /api/v1/keyvalue/{keyName}

        [Theory]
        [MemberData(nameof(GetReadAccessUsers))]
        public async Task GetKey_WithReadAccess_ShouldReturnKey(string userRole)
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var admin = _userUtility.GetAdminUsers().First();
            var keyName = "test-key-for-get";
            await CreateTestKeyAsync(admin, new CreateKeyDto { KeyName = keyName, Value = "test-value" });

            var user = GetUserByRole(userRole);

            // ACT
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            // CHANGED: Added namespaceId query parameter
            var response = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={user.NamespaceId}");

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var keyDto = await response.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(keyDto);
            Assert.Equal(keyName, keyDto.KeyName);
        }

        [Fact]
        public async Task GetKey_FromAnotherNamespace_ShouldReturnNotFound()
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var userA = _userUtility.GetNamespaceOwner();
            var keyName = "user-a-secret-key";
            await CreateTestKeyAsync(userA, new CreateKeyDto { KeyName = keyName, Value = "secret" });

            var userBUtility = new TestUserUtility(_client);
            await userBUtility.InitializeAsync();
            var userB = userBUtility.GetNamespaceOwner();

            // ACT
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userB.AccessToken);
            // CHANGED: Added namespaceId from the CALLER (User B)
            var response = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={userB.NamespaceId}");

            // ASSERT
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region PUT /api/v1/keyvalue/{keyName}

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task UpdateKey_WithWriteAccess_ShouldSucceed(string userRole)
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var admin = _userUtility.GetAdminUsers().First();
            var keyName = "key-to-update";
            await CreateTestKeyAsync(admin, new CreateKeyDto { KeyName = keyName, Value = "version1" });

            var user = GetUserByRole(userRole);
            var updateDto = new UpdateKeyDto { Value = "version2" };

            // ACT
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            // CHANGED: Added namespaceId query parameter
            var response = await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={user.NamespaceId}", updateDto);

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion

        #region History and Revert Workflow

        [Fact]
        public async Task FullWorkflow_CreateUpdateRevertHistory_ShouldWorkCorrectly()
        {
            // ARRANGE
            await _userUtility.InitializeAsync();
            var admin = _userUtility.GetAdminUsers().First();
            var keyName = "workflow-key";
            var namespaceId = admin.NamespaceId; // Get namespaceId for all calls
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

            // 1. CREATE
            // CHANGED: Added namespaceId query parameter
            await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={namespaceId}", new CreateKeyDto { KeyName = keyName, Value = "Value-V1" });

            // 2. UPDATE
            // CHANGED: Added namespaceId query parameter
            await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}", new UpdateKeyDto { Value = "Value-V2" });

            // 3. UPDATE AGAIN
            // CHANGED: Added namespaceId query parameter
            await _client.PutAsJsonAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}", new UpdateKeyDto { Value = "Value-V3" });

            // 4. GET HISTORY
            // CHANGED: Added namespaceId query parameter
            var historyResponse = await _client.GetAsync($"/api/v1/keyvalue/{keyName}/history?namespaceId={namespaceId}");
            historyResponse.EnsureSuccessStatusCode();
            var versions = await historyResponse.Content.ReadFromJsonAsync<List<VersionDto>>();
            Assert.NotNull(versions);
            Assert.Equal(3, versions.Count);

            // 5. REVERT
            var revertDto = new RevertKeyDto { KeyName = keyName, VersionNumber = 1 };
            // CHANGED: Added namespaceId query parameter
            var revertResponse = await _client.PostAsJsonAsync($"/api/v1/keyvalue/revert?namespaceId={namespaceId}", revertDto);
            revertResponse.EnsureSuccessStatusCode();

            // 6. VERIFY REVERT
            // CHANGED: Added namespaceId query parameter
            var finalGetResponse = await _client.GetAsync($"/api/v1/keyvalue/{keyName}?namespaceId={namespaceId}");
            finalGetResponse.EnsureSuccessStatusCode();
            var finalKey = await finalGetResponse.Content.ReadFromJsonAsync<KeyDto>();
            Assert.NotNull(finalKey);
            Assert.Equal("Value-V1", finalKey.CurrentValue);
            Assert.Equal(1, finalKey.CurrentVersionNumber);
        }

        #endregion

        #region Helper Methods

        private AuthenticatedUser GetUserByRole(string role)
        {
            return role switch
            {
                "readonly" => _userUtility.GetReadonlyUsers().First(),
                "editor" => _userUtility.GetEditorUsers().First(),
                "admin" => _userUtility.GetAdminUsers().First(),
                _ => throw new ArgumentOutOfRangeException(nameof(role), "Invalid role specified")
            };
        }

        private async Task<HttpResponseMessage> CreateTestKeyAsync(AuthenticatedUser user, CreateKeyDto dto)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            // CHANGED: Added namespaceId query parameter to the helper
            return await _client.PostAsJsonAsync($"/api/v1/keyvalue?namespaceId={user.NamespaceId}", dto);
        }

        #endregion
    }
}