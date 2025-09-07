using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IUserNamespaceRepository
    {
        public Task<IEnumerable<Namespace>> GetUserNamespacesAsync(int userId);
        public Task<NamespaceRole?> UserHasAccessToNamespaceAsync(int userId, int namespaceId);
        public Task GrantUserAccessToNamespaceAsync(int userId, int namespaceId, NamespaceRole role = NamespaceRole.ReadOnly);
        public Task RevokeUserAccessFromNamespaceAsync(int userId, int namespaceId);
        public Task<IEnumerable<UserNamespace>> GetUserNamespaceDetailsAsync(int userId);
        public Task<UserNamespace?> GetUserNamespaceEntryAsync(int userId, int namespaceId);
    }
}