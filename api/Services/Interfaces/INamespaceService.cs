using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface INamespaceService
    {
        Task<NamespaceDto?> GetNamespaceByIdAsync(int id);
        Task<NamespaceDto?> GetNamespaceByNameAsync(string name);
        Task<NamespaceDto> CreateNamespaceAsync(CreateNamespaceDto createNamespaceDto, int createdByUserId);
        Task<NamespaceDto> UpdateNamespaceAsync(int id, UpdateNamespaceDto updateNamespaceDto);
        Task DeleteNamespaceAsync(int id);
    }
}