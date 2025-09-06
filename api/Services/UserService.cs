using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RevloDB.Entities;

namespace RevloDB.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAPIKeyRepository _apiKeyRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IMapper _mapper;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            IAPIKeyRepository apiKeyRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _apiKeyRepository = apiKeyRepository;
            _mapper = mapper;
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user == null || user.IsDeleted ? null : _mapper.Map<UserDto>(user);
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                throw new InvalidOperationException("User not found");
            }

            await _userRepository.SoftDeleteAsync(userId);

            await _apiKeyRepository.SoftDeleteUserApiKeysAsync(userId);
        }
    }
}