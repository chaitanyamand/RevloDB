using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Extensions;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class UserNamespaceService : IUserNamespaceService
    {
        private readonly IUserNamespaceRepository _userNamespaceRepository;

        public UserNamespaceService(IUserNamespaceRepository userNamespaceRepository)
        {
            _userNamespaceRepository = userNamespaceRepository;
        }

        public async Task<IEnumerable<UserNamespaceDto>> GetUserNamespacesAsync(int userId)
        {
            var userNamespaceDetails = await _userNamespaceRepository.GetUserNamespaceDetailsAsync(userId);
            var userNamespaces = userNamespaceDetails.Select(detail => new UserNamespaceDto
            {
                UserId = detail.UserId,
                NamespaceId = detail.NamespaceId,
                Role = detail.Role.ToString(),
                JoinedAt = detail.GrantedAt,
                Namespace = new NamespaceDto
                {
                    Id = detail.Namespace.Id,
                    Name = detail.Namespace.Name,
                    Description = detail.Namespace.Description,
                    CreatedAt = detail.Namespace.CreatedAt
                }
            });

            return userNamespaces;
        }

        public async Task<NamespaceRole?> CheckUserAccessAsync(int userId, int namespaceId)
        {
            return await _userNamespaceRepository.UserHasAccessToNamespaceAsync(userId, namespaceId);
        }

        public async Task GrantUserAccessAsync(GrantAccessDto grantAccessDto)
        {
            var role = grantAccessDto.Role.ToEnumOrThrow<NamespaceRole>("Invalid role specified");
            await _userNamespaceRepository.GrantUserAccessToNamespaceAsync(
                grantAccessDto.UserId,
                grantAccessDto.NamespaceId,
                role
            );
        }

        public async Task RevokeUserAccessAsync(RevokeAccessDto revokeAccessDto)
        {
            await _userNamespaceRepository.RevokeUserAccessFromNamespaceAsync(
                revokeAccessDto.UserId,
                revokeAccessDto.NamespaceId
            );
        }
    }
}