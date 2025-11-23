using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RevloDB.API.Tests.DTOs;
using RevloDB.DTOs;

namespace RevloDB.API.Tests.Utilities
{
    public class TestUserUtility
    {
        private readonly HttpClient _client;
        private readonly ConcurrentDictionary<string, AuthenticatedUser> _userCache = new();
        private readonly SemaphoreSlim _namespaceSemaphore = new SemaphoreSlim(1, 1);

        private static AuthenticatedUser? _namespaceOwner;
        private static int? _namespaceId;
        private static readonly object _namespaceLock = new object();

        private const string NamespaceName = "Test-Namespace";

        public TestUserUtility(HttpClient client)
        {
            _client = client;
        }

        public async Task<AuthenticatedUser> GetUserAsync(string role)
        {
            if (_userCache.TryGetValue(role, out var cachedUser))
                return cachedUser;

            var user = await CreateAndLoginUserAsync(role);
            user.RoleInNamespace = role == "owner" ? "admin" : role;

            if (role == "owner")
            {
                await _namespaceSemaphore.WaitAsync();
                try
                {
                    if (_namespaceOwner == null)
                    {
                        _namespaceOwner = user;
                    }

                    if (_namespaceId == null)
                    {
                        _namespaceId = await CreateNamespaceAsync(_namespaceOwner, NamespaceName);
                    }

                    user.NamespaceId = _namespaceId.Value;
                    user.NamespaceName = NamespaceName;
                }
                finally
                {
                    _namespaceSemaphore.Release();
                }
            }
            else
            {
                await EnsureNamespaceOwnerAsync();

                await _namespaceSemaphore.WaitAsync();
                try
                {
                    await GrantAccessAsync(_namespaceOwner!, user.Id, _namespaceId!.Value, role);
                    user.NamespaceId = _namespaceId!.Value;
                    user.NamespaceName = NamespaceName;
                }
                finally
                {
                    _namespaceSemaphore.Release();
                }
            }

            return _userCache.AddOrUpdate(role, user, (key, existing) => existing);
        }

        public async Task<AuthenticatedUser> GetNamespaceOwnerAsync()
        {
            return await GetUserAsync("owner");
        }

        public async Task<AuthenticatedUser> GetReadonlyUserAsync()
        {
            return await GetUserAsync("readonly");
        }

        public async Task<AuthenticatedUser> GetEditorUserAsync()
        {
            return await GetUserAsync("editor");
        }

        public async Task<AuthenticatedUser> GetAdminUserAsync()
        {
            return await GetUserAsync("admin");
        }

        private async Task EnsureNamespaceOwnerAsync()
        {
            if (_namespaceOwner == null)
            {
                await _namespaceSemaphore.WaitAsync();
                try
                {
                    if (_namespaceOwner == null)
                    {
                        var owner = await CreateAndLoginUserAsync("owner");
                        owner.RoleInNamespace = "admin";
                        _namespaceOwner = owner;
                        _namespaceId = await CreateNamespaceAsync(owner, NamespaceName);
                    }
                }
                finally
                {
                    _namespaceSemaphore.Release();
                }
            }
        }

        private async Task<AuthenticatedUser> CreateAndLoginUserAsync(string rolePrefix)
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
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
    }
}