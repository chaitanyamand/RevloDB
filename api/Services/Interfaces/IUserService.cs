namespace RevloDB.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task DeleteUserAsync(int userId);
    }
}