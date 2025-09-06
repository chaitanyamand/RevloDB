using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;
using System.Security.Cryptography;

namespace RevloDB.Services
{
    public class UserAuthService : IUserAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public UserAuthService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            IJwtService jwtService,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        public async Task<UserDto> SignUpAsync(SignUpDto signUpDto)
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameOrEmailAsync(signUpDto.Username, signUpDto.Email);
            if (existingUser != null)
            {
                if (existingUser.Username.Equals(signUpDto.Username, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Username already exists");
                }
                throw new InvalidOperationException("Email already exists");
            }

            var user = new User
            {
                Username = signUpDto.Username,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, signUpDto.Password);

            var createdUser = await _userRepository.CreateAsync(user);
            return;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(loginDto.Username, loginDto.Username);

            if (user == null || user.IsDeleted)
            {
                return null;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                return null;
            }

            var accessToken = await _jwtService.GenerateAccessTokenAsync(user);

            return new LoginResponseDto
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
            };
        }
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, changePasswordDto.CurrentPassword);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, changePasswordDto.NewPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}