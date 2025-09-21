using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.Setup;
using RevloDB.DTOs;

namespace RevloDB.API.Tests
{
    public class UserAuthControllerTests : IClassFixture<ApiTestAppFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiTestAppFactory _factory;

        public UserAuthControllerTests(ApiTestAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        #region SignUp Tests

        [Fact]
        public async Task SignUp_WithValidData_ShouldCreateUser()
        {
            var signUpDto = new SignUpDto
            {
                Username = $"testuser_{Guid.NewGuid().ToString("N")[..8]}",
                Password = "ValidPassword@123",
                ConfirmPassword = "ValidPassword@123"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(userDto);
            Assert.Equal(signUpDto.Username, userDto.Username);
            Assert.True(userDto.Id > 0);
        }

        [Fact]
        public async Task SignUp_WithMismatchedPasswords_ShouldReturnBadRequest()
        {
            var signUpDto = new SignUpDto
            {
                Username = $"testuser_{Guid.NewGuid().ToString("N")[..8]}",
                Password = "ValidPassword@123",
                ConfirmPassword = "DifferentPassword@123"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SignUp_WithDuplicateUsername_ShouldReturnConflict()
        {
            var username = $"testuser_{Guid.NewGuid().ToString("N")[..8]}";
            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = "ValidPassword@123",
                ConfirmPassword = "ValidPassword@123"
            };

            await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);
            var duplicateResponse = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        }

        [Theory]
        [InlineData("", "ValidPassword@123", "ValidPassword@123")]
        [InlineData("validuser", "", "")]
        [InlineData("validuser", "weak", "weak")]
        [InlineData("validuser", "ValidPassword@123", "")]
        public async Task SignUp_WithInvalidData_ShouldReturnBadRequest(string username, string password, string confirmPassword)
        {
            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            var username = $"loginuser_{Guid.NewGuid().ToString("N")[..8]}";
            var password = "ValidPassword@123";

            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = password,
                ConfirmPassword = password
            };
            await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResponse);
            Assert.False(string.IsNullOrEmpty(loginResponse.AccessToken));
            Assert.True(loginResponse.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            var loginDto = new LoginDto
            {
                Username = "nonexistentuser",
                Password = "WrongPassword@123"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithCorrectUsernameWrongPassword_ShouldReturnUnauthorized()
        {
            var username = $"loginuser_{Guid.NewGuid().ToString("N")[..8]}";
            var password = "ValidPassword@123";

            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = password,
                ConfirmPassword = password
            };
            await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            var loginDto = new LoginDto
            {
                Username = username,
                Password = "WrongPassword@123"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("", "ValidPassword@123")]
        [InlineData("validuser", "")]
        [InlineData("", "")]
        public async Task Login_WithInvalidData_ShouldReturnBadRequest(string username, string password)
        {
            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldSucceed()
        {
            var (accessToken, _) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "ValidPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "NewValidPassword@456"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "ValidPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "NewValidPassword@456"
            };

            var response = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturnBadRequest()
        {
            var (accessToken, _) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongCurrentPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "NewValidPassword@456"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithMismatchedNewPasswords_ShouldReturnBadRequest()
        {
            var (accessToken, _) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "ValidPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "DifferentPassword@789"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_ThenLoginWithNewPassword_ShouldSucceed()
        {
            var (accessToken, username) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "ValidPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "NewValidPassword@456"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var changeResponse = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);
            Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Username = username,
                Password = "NewValidPassword@456"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResponseDto);
            Assert.False(string.IsNullOrEmpty(loginResponseDto.AccessToken));
        }

        [Fact]
        public async Task ChangePassword_ThenLoginWithOldPassword_ShouldFail()
        {
            var (accessToken, username) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "ValidPassword@123",
                NewPassword = "NewValidPassword@456",
                ConfirmNewPassword = "NewValidPassword@456"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var changeResponse = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);
            Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Username = username,
                Password = "ValidPassword@123"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        }

        [Theory]
        [InlineData("", "NewValidPassword@456", "NewValidPassword@456")]
        [InlineData("ValidPassword@123", "", "")]
        [InlineData("ValidPassword@123", "weak", "weak")]
        public async Task ChangePassword_WithInvalidData_ShouldReturnBadRequest(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var (accessToken, _) = await CreateAndLoginUserAsync();

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                ConfirmNewPassword = confirmNewPassword
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _client.PutAsJsonAsync("/api/v1/auth/password", changePasswordDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Helper Methods

        private async Task<(string accessToken, string username)> CreateAndLoginUserAsync()
        {
            var username = $"testuser_{Guid.NewGuid().ToString("N")[..8]}";
            var password = "ValidPassword@123";

            var signUpDto = new SignUpDto
            {
                Username = username,
                Password = password,
                ConfirmPassword = password
            };
            await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

            return (loginResponseDto!.AccessToken, username);
        }

        #endregion
    }
}