using Microsoft.AspNetCore.Mvc;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Controllers
{

    [Route("api/v1/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{userId}")]
        [AuthRequired]
        public async Task<ActionResult<UserDto>> GetUser(int userId)
        {
            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);

            if (currentUserId != userId)
            {
                return this.ForbiddenProblem("You can only access your own user information");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return this.NotFoundProblem($"User with ID {userId} not found");
            }

            return Ok(user);
        }

        [HttpDelete]
        [AuthRequired]
        public async Task<IActionResult> DeleteUser()
        {
            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            await _userService.DeleteUserAsync(currentUserId);
            return NoContent();
        }
    }
}