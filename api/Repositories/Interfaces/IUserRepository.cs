using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(string username, string passwordHash, string passwordSalt);
        Task UpdatePasswordAsync(string passwordHash, string passwordSalt, int id);
        Task DeleteAsync(int id);
    }
}