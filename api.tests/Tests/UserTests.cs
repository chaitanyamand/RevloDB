using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Setup;
using RevloDB.DTOs;

namespace RevloDB.API.Tests
{
    public class UserControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiTestAppFactory _factory;

        public UserControllerTests(ApiTestAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        #region GetUser Tests

        [Fact]
        public async Task GetUser_WithValidUserIdAndAuthentication_ShouldReturnUser()
        {
            var (accessToken, userId, username) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.GetAsync($"/api/v1/user/{userId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(userDto);
            Assert.Equal(userId, userDto.Id);
            Assert.Equal(username, userDto.Username);
        }

        [Fact]
        public async Task GetUser_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/v1/user/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_AccessingOtherUsersData_ShouldReturnForbidden()
        {
            var (accessToken1, userId1, _) = await CreateAndLoginUserAsync();
            var (_, userId2, _) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken1);
            var response = await _client.GetAsync($"/api/v1/user/{userId2}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_WithNonExistentUserId_ShouldReturnNotFound()
        {
            var (accessToken, _, _) = await CreateAndLoginUserAsync();
            var nonExistentUserId = 99999;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.GetAsync($"/api/v1/user/{nonExistentUserId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_WithInvalidUserId_ShouldReturnForbidden()
        {
            var (accessToken, _, _) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.GetAsync("/api/v1/user/invalid");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_WithInvalidToken_ShouldReturnUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.GetAsync("/api/v1/user/123");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_WithValidAuthentication_ShouldReturnNoContent()
        {
            var (accessToken, userId, _) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.DeleteAsync("/api/v1/user");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.DeleteAsync("/api/v1/user");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_WithInvalidToken_ShouldReturnUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var response = await _client.DeleteAsync("/api/v1/user");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_ThenTryToAccessUser_ShouldReturnNotFound()
        {
            var (accessToken, userId, _) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var deleteResponse = await _client.DeleteAsync("/api/v1/user");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/v1/user/{userId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_ThenTryToLogin_ShouldReturnUnauthorized()
        {
            var (accessToken, _, username) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var deleteResponse = await _client.DeleteAsync("/api/v1/user");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Username = username,
                Password = "ValidPassword@123"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_MultipleUsers_ShouldOnlyDeleteCurrentUser()
        {
            var (accessToken1, userId1, username1) = await CreateAndLoginUserAsync();
            var (accessToken2, userId2, username2) = await CreateAndLoginUserAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken1);
            var deleteResponse = await _client.DeleteAsync("/api/v1/user");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken2);
            var getResponse = await _client.GetAsync($"/api/v1/user/{userId2}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var userDto = await getResponse.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(userDto);
            Assert.Equal(userId2, userDto.Id);
            Assert.Equal(username2, userDto.Username);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task UserLifecycle_CreateLoginGetDelete_ShouldWorkCorrectly()
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
            Assert.Equal(HttpStatusCode.Created, signUpResponse.StatusCode);
            var createdUser = await signUpResponse.Content.ReadFromJsonAsync<UserDto>();

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponseDto!.AccessToken);
            var getUserResponse = await _client.GetAsync($"/api/v1/user/{createdUser!.Id}");
            Assert.Equal(HttpStatusCode.OK, getUserResponse.StatusCode);

            var deleteResponse = await _client.DeleteAsync("/api/v1/user");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var finalGetResponse = await _client.GetAsync($"/api/v1/user/{createdUser.Id}");
            Assert.Equal(HttpStatusCode.NotFound, finalGetResponse.StatusCode);
        }

        #endregion

        #region Helper Methods

        private async Task<(string accessToken, int userId, string username)> CreateAndLoginUserAsync()
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

            return (loginResponseDto!.AccessToken, userDto!.Id, username);
        }

        #endregion
    }
}