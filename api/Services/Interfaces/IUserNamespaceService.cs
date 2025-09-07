using RevloDB.DTOs;
using RevloDB.Entities;

namespace RevloDB.Services.Interfaces
{
    public interface IUserNamespaceService
    {
        Task<IEnumerable<UserNamespaceDto>> GetUserNamespacesAsync(int userId);
        Task<NamespaceRole?> CheckUserAccessAsync(int userId, int namespaceId);
        Task GrantUserAccessAsync(GrantAccessDto grantAccessDto);
        Task RevokeUserAccessAsync(RevokeAccessDto revokeAccessDto);
    }
}