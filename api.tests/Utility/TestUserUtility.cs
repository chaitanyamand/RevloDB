using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.DTOs;
using RevloDB.DTOs;

namespace RevloDB.API.Tests.Utilities
{
    public class TestUserUtility
    {
        private readonly HttpClient _client;
        private readonly List<AuthenticatedUser> _users = new();
        private AuthenticatedUser? _namespaceOwner;
        private int _namespaceId;
        private const string NamespaceName = "Test-Namespace";

        public TestUserUtility(HttpClient client)
        {
            _client = client;
        }

        public async Task InitializeAsync()
        {
            _namespaceOwner = await CreateAndLoginUserAsync("owner");
            _namespaceOwner.RoleInNamespace = "admin";

            var readonlyUsers = await CreateUsersForRoleAsync("readonly", 3, "readonly");
            var editorUsers = await CreateUsersForRoleAsync("editor", 3, "editor");
            var adminUsers = await CreateUsersForRoleAsync("admin", 2, "admin");

            _namespaceId = await CreateNamespaceAsync(_namespaceOwner, NamespaceName);

            _users.Add(_namespaceOwner);
            _users.AddRange(readonlyUsers);
            _users.AddRange(editorUsers);
            _users.AddRange(adminUsers);

            var allSecondaryUsers = readonlyUsers.Concat(editorUsers).Concat(adminUsers);
            foreach (var user in allSecondaryUsers)
            {
                await GrantAccessAsync(_namespaceOwner, user.Id, _namespaceId, user.RoleInNamespace);
            }

            foreach (var user in _users)
            {
                user.NamespaceId = _namespaceId;
                user.NamespaceName = NamespaceName;
            }
        }

        public AuthenticatedUser GetNamespaceOwner() => _namespaceOwner ?? throw new InvalidOperationException("Utility not initialized. Call InitializeAsync() first.");
        public IEnumerable<AuthenticatedUser> GetReadonlyUsers() => _users.Where(u => u.RoleInNamespace == "readonly");
        public IEnumerable<AuthenticatedUser> GetEditorUsers() => _users.Where(u => u.RoleInNamespace == "editor");
        public IEnumerable<AuthenticatedUser> GetAdminUsers() => _users.Where(u => u.RoleInNamespace == "admin");



        private async Task<AuthenticatedUser> CreateAndLoginUserAsync(string rolePrefix)
        {
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = new AuthenticatedUser
            {
                Username = $"{rolePrefix}_{uniqueId}",
                Password = "ValidPassword@123",
                RoleInNamespace = string.Empty,
                AccessToken = string.Empty,
                NamespaceName = string.Empty
            };

            var signUpDto = new SignUpDto { Username = user.Username, Password = user.Password, ConfirmPassword = user.Password };
            var signupResponse = await _client.PostAsJsonAsync("/api/v1/auth/signup", signUpDto);
            signupResponse.EnsureSuccessStatusCode();
            var createdUserDto = await signupResponse.Content.ReadFromJsonAsync<UserDto>();
            user.Id = createdUserDto!.Id;

            var loginDto = new LoginDto { Username = user.Username, Password = user.Password };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();
            var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            user.AccessToken = loginResponseDto!.AccessToken;

            return user;
        }

        private async Task<int> CreateNamespaceAsync(AuthenticatedUser owner, string namespaceName)
        {
            var createDto = new CreateNamespaceDto { Name = namespaceName };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/namespace", createDto);
            response.EnsureSuccessStatusCode();
            var namespaceDto = await response.Content.ReadFromJsonAsync<NamespaceDto>();
            _client.DefaultRequestHeaders.Authorization = null;
            return namespaceDto!.Id;
        }

        private async Task GrantAccessAsync(AuthenticatedUser admin, int targetUserId, int namespaceId, string role)
        {
            var grantDto = new GrantAccessDto { UserId = targetUserId, NamespaceId = namespaceId, Role = role };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var response = await _client.PostAsJsonAsync("/api/v1/user-namespaces/grant-access", grantDto);
            response.EnsureSuccessStatusCode();
            _client.DefaultRequestHeaders.Authorization = null;
        }

        private async Task<List<AuthenticatedUser>> CreateUsersForRoleAsync(string prefix, int count, string role)
        {
            var userTasks = new List<Task<AuthenticatedUser>>();
            for (int i = 0; i < count; i++)
            {
                userTasks.Add(CreateAndLoginUserAsync(prefix));
            }
            var users = await Task.WhenAll(userTasks);
            foreach (var user in users)
            {
                user.RoleInNamespace = role;
            }
            return users.ToList();
        }
    }
}