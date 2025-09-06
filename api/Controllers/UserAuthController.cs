using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Services.Interfaces;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Utils;

namespace RevloDB.Controllers
{
    [Route("api/v1/auth")]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserAuthService _userAuthService;

        public UserAuthController(IUserAuthService userAuthService)
        {
            _userAuthService = userAuthService;
        }

        [HttpPost("signup")]
        public async Task<ActionResult<UserDto>> SignUp([FromBody] SignUpDto signUpDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var user = await _userAuthService.SignUpAsync(signUpDto);
            return CreatedAtAction(nameof(UserController.GetUser), new { userId = user.Id }, user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var loginResponse = await _userAuthService.LoginAsync(loginDto);
            if (loginResponse == null)
            {
                return this.UnauthorizedProblem("Invalid username or password");
            }

            return Ok(loginResponse);
        }

        [HttpPut("password")]
        [AuthRequired]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);

            await _userAuthService.ChangePasswordAsync(currentUserId, changePasswordDto);
            return NoContent();
        }
    }
}
