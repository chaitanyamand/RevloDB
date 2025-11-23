using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Setup;
using RevloDB.API.Tests.Utilities;
using RevloDB.DTOs;

namespace RevloDB.API.Tests
{
    public class UserNamespaceControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly TestUserUtility _userUtility;
        private readonly ApiTestAppFactory _factory;

        public UserNamespaceControllerTests(ApiTestAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _userUtility = new TestUserUtility(_client);
        }

        #region GetUserNamespaces Tests

        [Fact]
        public async Task GetUserNamespaces_WithAuthentication_ShouldReturnUserNamespaces()
        {
            var user = await _userUtility.GetReadonlyUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/user/{user.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userNamespaces = await response.Content.ReadFromJsonAsync<IEnumerable<UserNamespaceDto>>();
            Assert.NotNull(userNamespaces);
            Assert.Contains(userNamespaces, un => un.NamespaceId == user.NamespaceId);
        }

        [Fact]
        public async Task GetUserNamespaces_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/user-namespaces/user/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserNamespaces_WithInvalidToken_ShouldReturnUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.GetAsync("/api/v1/user-namespaces/user/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion

        #region CheckUserAccessForRole Tests

        [Theory]
        [InlineData("readonly")]
        [InlineData("editor")]
        [InlineData("admin")]
        public async Task CheckUserAccessForRole_WithValidUserAndRole_ShouldReturnAccess(string roleToCheck)
        {
            var user = await _userUtility.GetUserAsync(roleToCheck);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{user.NamespaceId}?role={roleToCheck}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CheckUserAccessForRole_ReadonlyUserCheckingAdminRole_ShouldReturnNoAccess()
        {
            var user = await _userUtility.GetReadonlyUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{user.NamespaceId}?role=admin");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"hasAccess\":false", content);
        }

        [Fact]
        public async Task CheckUserAccessForRole_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/user-namespaces/access-check/1?role=readonly");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CheckUserAccessForRole_WithInvalidRole_ShouldReturnBadRequest()
        {
            var user = await _userUtility.GetReadonlyUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{user.NamespaceId}?role=invalidrole");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CheckUserAccessForRole_WithInvalidNamespaceId_ShouldReturnBadRequest(int namespaceId)
        {
            var user = await _userUtility.GetReadonlyUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{namespaceId}?role=readonly");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CheckUserAccessForRole_WithNonExistentNamespace_ShouldReturnNoAccess()
        {
            var user = await _userUtility.GetReadonlyUserAsync();
            var nonExistentNamespaceId = 99999;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            var response = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{nonExistentNamespaceId}?role=readonly");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"hasAccess\":false", content);
        }

        #endregion

        #region GrantUserAccess Tests

        [Fact]
        public async Task GrantUserAccess_WithAdminUser_ShouldSucceed()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var newUser = await CreateStandaloneUserAsync();

            var grantAccessDto = new GrantAccessDto
            {
                UserId = newUser.userId,
                NamespaceId = admin.NamespaceId,
                Role = "readonly"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.PostAsJsonAsync($"/api/v1/user-namespaces/grant-access?namespaceId={admin.NamespaceId}", grantAccessDto);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GrantUserAccess_WithNonAdminUser_ShouldReturnForbidden()
        {
            var editor = await _userUtility.GetEditorUserAsync();
            var newUser = await CreateStandaloneUserAsync();

            var grantAccessDto = new GrantAccessDto
            {
                UserId = newUser.userId,
                NamespaceId = editor.NamespaceId,
                Role = "readonly"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", editor.AccessToken);
            var response = await _client.PostAsJsonAsync($"/api/v1/user-namespaces/grant-access?namespaceId={editor.NamespaceId}", grantAccessDto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GrantUserAccess_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var grantAccessDto = new GrantAccessDto
            {
                UserId = 123,
                NamespaceId = 456,
                Role = "readonly"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/user-namespaces/grant-access?namespaceId=456", grantAccessDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData(0, 1, "readonly")]
        [InlineData(1, 0, "readonly")]
        public async Task GrantUserAccess_WithInvalidUserOrNamespaceId_ShouldReturnNotFound(int userId, int namespaceId, string role)
        {
            var admin = await _userUtility.GetAdminUserAsync();

            var grantAccessDto = new GrantAccessDto
            {
                UserId = userId,
                NamespaceId = namespaceId,
                Role = role
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.PostAsJsonAsync($"/api/v1/user-namespaces/grant-access?namespaceId={admin.NamespaceId}", grantAccessDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalidrole")]
        public async Task GrantUserAccess_WithInvalidRole_ShouldReturnBadRequest(string role)
        {
            var admin = await _userUtility.GetAdminUserAsync();

            var grantAccessDto = new GrantAccessDto
            {
                UserId = 1,
                NamespaceId = 1,
                Role = role
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.PostAsJsonAsync($"/api/v1/user-namespaces/grant-access?namespaceId={admin.NamespaceId}", grantAccessDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region RevokeUserAccess Tests

        [Fact]
        public async Task RevokeUserAccess_WithAdminUser_ShouldSucceed()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var targetUser = await _userUtility.GetReadonlyUserAsync();

            var revokeAccessDto = new RevokeAccessDto
            {
                UserId = targetUser.Id,
                NamespaceId = admin.NamespaceId
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/user-namespaces/revoke-access?namespaceId={admin.NamespaceId}")
            {
                Content = JsonContent.Create(revokeAccessDto)
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RevokeUserAccess_WithNonAdminUser_ShouldReturnForbidden()
        {
            var editor = await _userUtility.GetEditorUserAsync();
            var targetUser = await _userUtility.GetReadonlyUserAsync();

            var revokeAccessDto = new RevokeAccessDto
            {
                UserId = targetUser.Id,
                NamespaceId = editor.NamespaceId
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", editor.AccessToken);
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/user-namespaces/revoke-access?namespaceId={editor.NamespaceId}")
            {
                Content = JsonContent.Create(revokeAccessDto)
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RevokeUserAccess_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var revokeAccessDto = new RevokeAccessDto
            {
                UserId = 123,
                NamespaceId = 456
            };

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/user-namespaces/revoke-access?namespaceId=456")
            {
                Content = JsonContent.Create(revokeAccessDto)
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(0, 0)]
        public async Task RevokeUserAccess_WithInvalidData_ShouldReturnUnauthorized(int userId, int namespaceId)
        {
            var admin = await _userUtility.GetAdminUserAsync();

            var revokeAccessDto = new RevokeAccessDto
            {
                UserId = userId,
                NamespaceId = namespaceId
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/user-namespaces/revoke-access?namespaceId={admin.NamespaceId}")
            {
                Content = JsonContent.Create(revokeAccessDto)
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullAccessManagementWorkflow_GrantThenRevoke_ShouldWorkCorrectly()
        {
            var admin = await _userUtility.GetAdminUserAsync();
            var newUser = await CreateStandaloneUserAsync();

            var grantAccessDto = new GrantAccessDto
            {
                UserId = newUser.userId,
                NamespaceId = admin.NamespaceId,
                Role = "editor"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var grantResponse = await _client.PostAsJsonAsync($"/api/v1/user-namespaces/grant-access?namespaceId={admin.NamespaceId}", grantAccessDto);
            Assert.Equal(HttpStatusCode.NoContent, grantResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUser.accessToken);
            var checkResponse = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{admin.NamespaceId}?role=editor");
            Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
            var content = await checkResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("\"hasAccess\":false", content);

            var revokeAccessDto = new RevokeAccessDto
            {
                UserId = newUser.userId,
                NamespaceId = admin.NamespaceId
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var revokeResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/user-namespaces/revoke-access?namespaceId={admin.NamespaceId}")
            {
                Content = JsonContent.Create(revokeAccessDto)
            });
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUser.accessToken);
            var finalCheckResponse = await _client.GetAsync($"/api/v1/user-namespaces/access-check/{admin.NamespaceId}?role=readonly");
            Assert.Equal(HttpStatusCode.OK, finalCheckResponse.StatusCode);
            var finalContent = await finalCheckResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"hasAccess\":false", finalContent);
        }

        #endregion

        #region Helper Methods

        private async Task<(int userId, string accessToken)> CreateStandaloneUserAsync()
        {
            var username = $"testuser_{Guid.NewGuid().ToString("N")[..8]}";
            var password = "ValidPassword@123";

            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = password,
                ConfirmPassword = password
            };
            var signUpResponse = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);
            var userDto = await signUpResponse.Content.ReadFromJsonAsync<UserDto>();

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

            return (userDto!.Id, loginResponseDto!.AccessToken);
        }

        #endregion
    }
}