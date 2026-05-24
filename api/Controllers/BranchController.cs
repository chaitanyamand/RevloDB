using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Controllers
{
    [Route("api/v1/[controller]")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<List<BranchDto>>> GetAllBranches()
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            var branches = await _branchService.GetAllBranchesAsync(namespaceId);
            return Ok(branches);
        }

        [HttpPost]
        [AuthRequired]
        [Write]
        public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] CreateBranchDto createBranchDto)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            if (!ModelState.IsValid)
                return this.ModelValidationProblem(ModelState);

            try
            {
                var branch = await _branchService.CreateBranchAsync(createBranchDto, namespaceId);
                return CreatedAtAction(nameof(GetAllBranches), new { }, branch);
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

        [HttpDelete("{name}")]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> DeleteBranch(string name)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
                return this.BadRequestProblem("Namespace ID must be a positive integer");

            try
            {
                await _branchService.DeleteBranchAsync(name, namespaceId);
                return NoContent();
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
