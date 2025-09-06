using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IUserAuthService
    {
        Task<UserDto> SignUpAsync(SignUpDto signUpDto);
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        public Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    }
}