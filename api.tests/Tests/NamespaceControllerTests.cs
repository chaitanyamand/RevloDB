using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Utilities;
using RevloDB.DTOs;
using RevloDB.API.Tests.Setup;
using RevloDB.API.Tests.DTOs;
using Xunit.Abstractions;

namespace RevloDB.API.Tests
{
    public class NamespaceControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly TestUserUtility _userUtility;
        private readonly ApiTestAppFactory _factory;
        private ITestOutputHelper _output;

        public NamespaceControllerTests(ApiTestAppFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _output = output;
            _userUtility = new TestUserUtility(_client);
        }

        public static IEnumerable<object[]> GetWriteAccessUsers()
        {
            yield return new object[] { "editor" };
            yield return new object[] { "admin" };
        }

        public static IEnumerable<object[]> GetAllAccessUsers()
        {
            yield return new object[] { "readonly" };
            yield return new object[] { "editor" };
            yield return new object[] { "admin" };
        }

        #region CreateNamespace Tests

        [Fact]
        public async Task CreateNamespace_WithValidData_ShouldSucceed()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();
            var namespaceName = $"test-namespace-{Guid.NewGuid().ToString("N")[..8]}";
            var createDto = new CreateNamespaceDto
            {
                Name = namespaceName,
                Description = "Test namespace description"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdNamespace = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(createdNamespace);
            Assert.Equal(namespaceName, createdNamespace.Name);
            Assert.Equal("Test namespace description", createdNamespace.Description);
            Assert.True(createdNamespace.Id > 0);
        }

        [Fact]
        public async Task CreateNamespace_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var createDto = new CreateNamespaceDto
            {
                Name = "unauthorized-namespace",
                Description = "Should not be created"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateNamespace_WithEmptyName_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();
            var createDto = new CreateNamespaceDto { Name = "", Description = "Empty name test" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateNamespace_WithNameTooLong_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();
            var longName = new string('a', 101); // Exceeds 100 character limit
            var createDto = new CreateNamespaceDto { Name = longName, Description = "Long name test" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateNamespace_WithDescriptionTooLong_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();
            var namespaceName = $"test-namespace-{Guid.NewGuid().ToString("N")[..8]}";
            var longDescription = new string('a', 501); // Exceeds 500 character limit
            var createDto = new CreateNamespaceDto { Name = namespaceName, Description = longDescription };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region GetNamespace Tests

        [Theory]
        [MemberData(nameof(GetAllAccessUsers))]
        public async Task GetNamespace_WithValidAccess_ShouldReturnNamespace(string userRole)
        {
            var user = await _userUtility.GetUserAsync(userRole);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/namespace?namespaceId={user.NamespaceId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var namespaceDto = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(namespaceDto);
            Assert.Equal(user.NamespaceId, namespaceDto.Id);
            Assert.Equal(user.NamespaceName, namespaceDto.Name);
        }

        [Fact]
        public async Task GetNamespace_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/namespace?namespaceId=1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetNamespace_WithInvalidNamespaceId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/namespace?namespaceId=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNamespace_WithNegativeNamespaceId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/namespace?namespaceId=-1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNamespace_WithNonExistentNamespaceId_ShouldReturnNotFound()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/namespace?namespaceId=99999");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        #endregion

        #region GetNamespaceByName Tests

        [Theory]
        [MemberData(nameof(GetAllAccessUsers))]
        public async Task GetNamespaceByName_WithValidAccess_ShouldReturnNamespace(string userRole)
        {
            var user = await _userUtility.GetUserAsync(userRole);

            _output.WriteLine($"User Role: {userRole}");  // <-- Use it
            _output.WriteLine($"Namespace: {user.NamespaceName}");
            _output.WriteLine($"Namespace ID: {user.NamespaceId}");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/namespace/by-name/{user.NamespaceName}");

            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response: {responseBody}");  // <-- Use it

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var namespaceDto = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(namespaceDto);
            Assert.Equal(user.NamespaceId, namespaceDto.Id);
            Assert.Equal(user.NamespaceName, namespaceDto.Name);
        }

        [Fact]
        public async Task GetNamespaceByName_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/namespace/by-name/test-namespace");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetNamespaceByName_WithEmptyName_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/namespace/by-name/ ");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNamespaceByName_WithNonExistentName_ShouldReturnNotFound()
        {
            var user = await _userUtility.GetNamespaceOwnerAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync("/api/v1/namespace/by-name/nonexistent-namespace");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region UpdateNamespace Tests

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task UpdateNamespace_WithWriteAccess_ShouldSucceed(string userRole)
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var user = await _userUtility.GetUserAsync(userRole);
            var namespaceToUpdate = await CreateTestNamespaceWithUserAccessAsync(admin, userRole, user);

            var updateDto = new UpdateNamespaceDto
            {
                Name = $"updated-{namespaceToUpdate.Name}",
                Description = "Updated description"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PutAsJsonAsync($"/api/v1/namespace/?namespaceId={namespaceToUpdate.Id}", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedNamespace = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(updatedNamespace);
            Assert.Equal(updateDto.Name, updatedNamespace.Name);
            Assert.Equal(updateDto.Description, updatedNamespace.Description);
        }

        [Fact]
        public async Task UpdateNamespace_WithReadonlyUser_ShouldBeForbidden()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var readonlyUser = await _userUtility.GetReadonlyUserAsync();
            var namespaceToUpdate = await CreateTestNamespaceWithUserAccessAsync(admin, "readonly", readonlyUser);

            var updateDto = new UpdateNamespaceDto
            {
                Name = "forbidden-update",
                Description = "Should not be updated"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readonlyUser.AccessToken);
            var response = await _client.PutAsJsonAsync($"/api/v1/namespace/?namespaceId={namespaceToUpdate.Id}", updateDto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateNamespace_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var updateDto = new UpdateNamespaceDto
            {
                Name = "unauthorized-update",
                Description = "Should not be updated"
            };

            var response = await _client.PutAsJsonAsync("/api/v1/namespace/?namespaceId=1", updateDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateNamespace_WithInvalidId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();
            var updateDto = new UpdateNamespaceDto { Name = "test-name", Description = "test description" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PutAsJsonAsync("/api/v1/namespace/?namespaceId=0", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateNamespace_WithEmptyName_ShouldReturnBadRequest()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var namespaceToUpdate = await CreateTestNamespaceAsync(admin);
            var updateDto = new UpdateNamespaceDto { Name = "", Description = "Empty name test" };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.PutAsJsonAsync($"/api/v1/namespace/?namespaceId={namespaceToUpdate.Id}", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region DeleteNamespace Tests

        [Theory]
        [MemberData(nameof(GetWriteAccessUsers))]
        public async Task DeleteNamespace_WithWriteAccess_ShouldSucceed(string userRole)
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var user = await _userUtility.GetUserAsync(userRole);
            var namespaceToDelete = await CreateTestNamespaceWithUserAccessAsync(admin, userRole, user);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.DeleteAsync($"/api/v1/namespace/?namespaceId={namespaceToDelete.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNamespace_WithReadonlyUser_ShouldBeForbidden()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var readonlyUser = await _userUtility.GetReadonlyUserAsync();
            var namespaceToDelete = await CreateTestNamespaceWithUserAccessAsync(admin, "readonly", readonlyUser);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readonlyUser.AccessToken);
            var response = await _client.DeleteAsync($"/api/v1/namespace/?namespaceId={namespaceToDelete.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNamespace_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.DeleteAsync("/api/v1/namespace/?namespaceId=1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNamespace_WithInvalidId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.DeleteAsync("/api/v1/namespace/?namespaceId=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNamespace_WithNegativeId_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetAdminUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.DeleteAsync("/api/v1/namespace/?namespaceId=-1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Full Workflow Tests

        [Fact]
        public async Task FullWorkflow_CreateGetUpdateDelete_ShouldWorkCorrectly()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var namespaceName = $"workflow-namespace-{Guid.NewGuid().ToString("N")[..8]}";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

            // Create
            var createDto = new CreateNamespaceDto
            {
                Name = namespaceName,
                Description = "Initial description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);
            createResponse.EnsureSuccessStatusCode();
            var createdNamespace = await createResponse.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(createdNamespace);

            // Get by ID
            var getResponse = await _client.GetAsync($"/api/v1/namespace?namespaceId={createdNamespace.Id}");
            getResponse.EnsureSuccessStatusCode();
            var fetchedNamespace = await getResponse.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(fetchedNamespace);
            Assert.Equal(namespaceName, fetchedNamespace.Name);

            // Get by Name
            var getByNameResponse = await _client.GetAsync($"/api/v1/namespace/by-name/{namespaceName}");
            getByNameResponse.EnsureSuccessStatusCode();
            var fetchedByName = await getByNameResponse.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(fetchedByName);
            Assert.Equal(createdNamespace.Id, fetchedByName.Id);

            // Update
            var updateDto = new UpdateNamespaceDto
            {
                Name = $"updated-{namespaceName}",
                Description = "Updated description"
            };
            var updateResponse = await _client.PutAsJsonAsync($"/api/v1/namespace/?namespaceId={createdNamespace.Id}", updateDto);
            updateResponse.EnsureSuccessStatusCode();
            var updatedNamespace = await updateResponse.Content.ReadFromJsonAsync<NamespaceDto>();
            Assert.NotNull(updatedNamespace);
            Assert.Equal(updateDto.Name, updatedNamespace.Name);
            Assert.Equal(updateDto.Description, updatedNamespace.Description);

            // Delete
            var deleteResponse = await _client.DeleteAsync($"/api/v1/namespace/?namespaceId={createdNamespace.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // Verify deletion
            var getAfterDeleteResponse = await _client.GetAsync($"/api/v1/namespace?namespaceId={createdNamespace.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
        }

        #endregion

        #region Helper Methods

        private async Task<NamespaceDto> CreateTestNamespaceAsync(AuthenticatedUser user)
        {
            var namespaceName = $"test-namespace-{Guid.NewGuid().ToString("N")[..8]}";
            var createDto = new CreateNamespaceDto
            {
                Name = namespaceName,
                Description = "Test namespace for operations"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);
            response.EnsureSuccessStatusCode();
            var createdNamespace = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            _client.DefaultRequestHeaders.Authorization = null;

            return createdNamespace!;
        }

        private async Task<NamespaceDto> CreateTestNamespaceWithUserAccessAsync(AuthenticatedUser admin, string userRole, AuthenticatedUser targetUser)
        {
            // Create the namespace
            var testNamespace = await CreateTestNamespaceAsync(admin);

            // Grant access to the target user for this specific namespace
            var grantAccessDto = new GrantAccessDto
            {
                UserId = targetUser.Id,
                NamespaceId = testNamespace.Id,
                Role = userRole
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var grantResponse = await _client.PostAsJsonAsync("/api/v1/user-namespaces/grant-access", grantAccessDto);
            grantResponse.EnsureSuccessStatusCode();
            _client.DefaultRequestHeaders.Authorization = null;

            return testNamespace;
        }

        #endregion
    }
}