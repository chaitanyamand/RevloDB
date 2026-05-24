using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Exceptions;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Controllers
{
    [ApiController]
    [Route("api/v1/branches/{branchName}/merge")]
    public class MergeController : ControllerBase
    {
        private readonly IMergeService _mergeService;

        public MergeController(IMergeService mergeService)
        {
            _mergeService = mergeService;
        }

        [HttpPost]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> Merge(string branchName, [FromBody] MergeRequestDto request)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);

            if (!ModelState.IsValid)
                return this.ModelValidationProblem(ModelState);

            if (string.Equals(branchName, request.SourceBranchName, StringComparison.OrdinalIgnoreCase))
                return this.BadRequestProblem("Cannot merge a branch into itself.");

            try
            {
                var result = await _mergeService.MergeAsync(branchName, request, namespaceId, userId);

                if (!result.Success)
                {
                    return Conflict(new { error = "Merge conflict detected.", data = result });
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return this.NotFoundProblem(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return this.BadRequestProblem(ex.Message);
            }
            catch (ConcurrentMergeException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return this.BadRequestProblem("An unexpected error occurred during merge. " + ex.Message);
            }
        }
    }
}
