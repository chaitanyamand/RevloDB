using AutoMapper;
using RevloDB.Configuration;
using RevloDB.DTOs;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;
using System.Text.RegularExpressions;

namespace RevloDB.Services
{
    public class UserAuthService : IUserAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthOptions _authOptions;
        private readonly IMapper _mapper;

        public UserAuthService(
            IUserRepository userRepository,
            IMapper mapper,
            AuthOptions authOptions)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _authOptions = authOptions;
        }

        public async Task<UserDto> SignUpAsync(SignUpDto signUpDto)
        {
            var (username, userPassword) = ValidateSignupInput(signUpDto);
            (string hashedPassword, string salt) = HashUtil.HashPassword(userPassword);
            var user = await _userRepository.CreateAsync(username, hashedPassword, salt);
            var userDTO = _mapper.Map<UserDto>(user);
            return userDTO;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var loginUsername = loginDto.Username.Trim();
            var loginPassword = loginDto.Password.Trim();
            var user = await _userRepository.GetByUsernameAsync(loginUsername);

            if (user == null || user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var verificationResult = HashUtil.VerifyPassword(loginPassword, user.PasswordHash, user.PasswordSalt);
            if (!verificationResult)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var accessToken = JwtUtil.GenerateToken(_authOptions.Jwt.Key, new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, _authOptions.Jwt.ExpirationInSeconds);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(_authOptions.Jwt.ExpirationInSeconds),
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var currentPassword = changePasswordDto.CurrentPassword.Trim();
            var newPassword = changePasswordDto.NewPassword.Trim();
            (bool newPasswordValid, string? newPasswordError) = ValidatePassword(newPassword);
            if (!newPasswordValid)
            {
                throw new BadHttpRequestException(newPasswordError ?? "Invalid new password");
            }
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                throw new KeyNotFoundException("No user found");
            }

            var verificationResult = HashUtil.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt);
            if (!verificationResult)
            {
                throw new BadHttpRequestException("Incorrect current password");
            }

            (string hashedNewPassword, string newPasswordSalt) = HashUtil.HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(hashedNewPassword, newPasswordSalt, user.Id);
        }

        private (bool IsValid, string? Error) ValidateUsername(string username)
        {
            if (username.Length < 3)
            {
                return (false, "Username must be at least 3 characters long");
            }
            if (!Regex.IsMatch(username, "^[a-z]"))
            {
                return (false, "Username must start with a letter");
            }
            if (!Regex.IsMatch(username, "^[a-z0-9._]+$"))
            {
                return (false, "Username can only contain letters, numbers, dots, and underscores");
            }
            if (Regex.IsMatch(username, @"[._]{2,}"))
            {
                return (false, "Username cannot contain consecutive dots or underscores");
            }
            if (Regex.IsMatch(username, @"[._]$"))
            {
                return (false, "Username cannot end with a dot or underscore");
            }
            return (true, null);
        }

        private (bool IsValid, string? Error) ValidatePassword(string password)
        {
            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                return (false, "Password must contain at least one uppercase letter");
            }
            if (!Regex.IsMatch(password, "[a-z]"))
            {
                return (false, "Password must contain at least one lowercase letter");
            }
            if (!Regex.IsMatch(password, "[0-9]"))
            {
                return (false, "Password must contain at least one digit");
            }
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            {
                return (false, "Password must contain at least one special character");
            }
            return (true, null);
        }

        private (string username, string password) ValidateSignupInput(SignUpDto signUpDto)
        {
            var username = signUpDto.Username.Trim();
            var userPassword = signUpDto.Password.Trim();
            var (isUsernameValid, usernameError) = ValidateUsername(username);
            var (isPasswordValid, passwordError) = ValidatePassword(userPassword);
            if (!isUsernameValid)
            {
                throw new BadHttpRequestException(usernameError ?? "Invalid username");
            }
            if (!isPasswordValid)
            {
                throw new BadHttpRequestException(passwordError ?? "Invalid password");
            }
            return (username, userPassword);
        }

    }
}