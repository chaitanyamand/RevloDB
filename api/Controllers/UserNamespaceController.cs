using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Services.Interfaces;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Utility;
using RevloDB.Entities;

namespace RevloDB.Controllers
{
    [Route("api/v1/user-namespaces")]
    public class UserNamespaceController : ControllerBase
    {
        private readonly IUserNamespaceService _userNamespaceService;

        public UserNamespaceController(IUserNamespaceService userNamespaceService)
        {
            _userNamespaceService = userNamespaceService;
        }

        [HttpGet("user/{userId}")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<IEnumerable<UserNamespaceDto>>> GetUserNamespaces()
        {
            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var userNamespaces = await _userNamespaceService.GetUserNamespacesAsync(userId);
            return Ok(userNamespaces);
        }

        [HttpGet("access-check/{namespaceId}")]
        [AuthRequired]
        public async Task<ActionResult<object>> CheckUserAccessForRole(int namespaceId, [FromQuery] string role)
        {
            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var enumRole = role.ToEnumOrThrow<NamespaceRole>("Invalid role specified");
            if (userId <= 0)
            {
                return this.BadRequestProblem("User ID must be a positive integer");
            }

            if (namespaceId <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            var userRole = await _userNamespaceService.CheckUserAccessAsync(userId, namespaceId);
            if (!userRole.HasValue || RoleCheckUtil.HasSufficientRole(userRole.Value, enumRole) == false)
            {
                return Ok(new { hasAccess = false });
            }

            return Ok(new { UserId = userId, NamespaceId = namespaceId, Role = userRole });
        }

        [HttpPost("grant-access")]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> GrantUserAccess([FromBody] GrantAccessDto grantAccessDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            await _userNamespaceService.GrantUserAccessAsync(grantAccessDto);
            return NoContent();
        }

        [HttpDelete("revoke-access")]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> RevokeUserAccess([FromBody] RevokeAccessDto revokeAccessDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            await _userNamespaceService.RevokeUserAccessAsync(revokeAccessDto);
            return NoContent();
        }
    }
}