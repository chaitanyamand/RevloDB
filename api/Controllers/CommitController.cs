using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Controllers
{
    [Route("api/v1/branches/{branchName}/commits")]
    public class CommitController : ControllerBase
    {
        private readonly ICommitService _commitService;

        public CommitController(ICommitService commitService)
        {
            _commitService = commitService;
        }

        [HttpGet]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<List<CommitDto>>> GetHistory(string branchName, [FromQuery] int limit = 50)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            try
            {
                var history = await _commitService.GetHistoryAsync(branchName, namespaceId, limit);
                return Ok(history);
            }
            catch (KeyNotFoundException ex)
            {
                return this.NotFoundProblem(ex.Message);
            }
        }

        [HttpGet("{hash}")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<CommitDto>> GetCommit(string branchName, string hash)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            try
            {
                var commit = await _commitService.GetByHashAsync(hash, branchName, namespaceId);
                if (commit == null)
                    return this.NotFoundProblem($"Commit '{hash}' not found.");

                return Ok(commit);
            }
            catch (KeyNotFoundException ex)
            {
                return this.NotFoundProblem(ex.Message);
            }
        }

        [HttpPost]
        [AuthRequired]
        [Write]
        public async Task<ActionResult<CommitDto>> CreateCommit(string branchName, [FromBody] CreateCommitDto createCommitDto)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);

            if (!ModelState.IsValid)
                return this.ModelValidationProblem(ModelState);

            try
            {
                var commit = await _commitService.CreateCommitAsync(branchName, createCommitDto, namespaceId, userId);
                return CreatedAtAction(nameof(GetCommit), new { branchName = branchName, hash = commit.Hash }, commit);
            }
            catch (KeyNotFoundException ex)
            {
                return this.NotFoundProblem(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return this.BadRequestProblem(ex.Message);
            }
        }
    }
}
