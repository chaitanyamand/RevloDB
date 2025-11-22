using RevloDB.DTOs;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class NamespaceService : INamespaceService
    {
        private readonly INamespaceRepository _namespaceRepository;

        public NamespaceService(INamespaceRepository namespaceRepository)
        {
            _namespaceRepository = namespaceRepository;
        }

        public async Task<NamespaceDto?> GetNamespaceByIdAsync(int id)
        {
            var namespaceEntity = await _namespaceRepository.GetByIdAsync(id);
            if (namespaceEntity == null) return null;

            return new NamespaceDto
            {
                Id = namespaceEntity.Id,
                Name = namespaceEntity.Name,
                Description = namespaceEntity.Description,
                CreatedAt = namespaceEntity.CreatedAt
            };
        }

        public async Task<NamespaceDto?> GetNamespaceByNameAsync(string name)
        {
            var namespaceEntity = await _namespaceRepository.GetByNameAsync(name);
            if (namespaceEntity == null) return null;

            return new NamespaceDto
            {
                Id = namespaceEntity.Id,
                Name = namespaceEntity.Name,
                Description = namespaceEntity.Description,
                CreatedAt = namespaceEntity.CreatedAt
            };
        }

        public async Task<NamespaceDto> CreateNamespaceAsync(CreateNamespaceDto createNamespaceDto, int createdByUserId)
        {
            var namespaceEntity = await _namespaceRepository.CreateAsync(
                createNamespaceDto.Name,
                createNamespaceDto.Description,
                createdByUserId
            );

            return new NamespaceDto
            {
                Id = namespaceEntity.Id,
                Name = namespaceEntity.Name,
                Description = namespaceEntity.Description,
                CreatedAt = namespaceEntity.CreatedAt
            };
        }

        public async Task<NamespaceDto> UpdateNamespaceAsync(int id, UpdateNamespaceDto updateNamespaceDto)
        {
            var updatedNamespace = await _namespaceRepository.UpdateNameAsync(updateNamespaceDto.Name, id);
            if (updatedNamespace == null)
            {
                throw new InvalidOperationException($"Namespace with id {id} not found after update");
            }

            return new NamespaceDto
            {
                Id = updatedNamespace.Id,
                Name = updatedNamespace.Name,
                Description = updatedNamespace.Description,
                CreatedAt = updatedNamespace.CreatedAt
            };
        }

        public async Task DeleteNamespaceAsync(int id)
        {
            await _namespaceRepository.DeleteAsync(id);
        }
    }
}