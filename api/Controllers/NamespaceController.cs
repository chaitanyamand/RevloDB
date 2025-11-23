using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Services.Interfaces;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Utility;

namespace RevloDB.Controllers
{
    [Route("api/v1/[controller]")]
    public class NamespaceController : ControllerBase
    {
        private readonly INamespaceService _namespaceService;

        public NamespaceController(INamespaceService namespaceService)
        {
            _namespaceService = namespaceService;
        }

        [HttpGet]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<NamespaceDto>> GetNamespace()
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            var namespaceDto = await _namespaceService.GetNamespaceByIdAsync(namespaceId);
            if (namespaceDto == null)
            {
                return this.NotFoundProblem($"Namespace with id {namespaceId} not found");
            }

            return Ok(namespaceDto);
        }

        [HttpGet("by-name/{**name}")]
        [AuthRequired]
        public async Task<ActionResult<NamespaceDto>> GetNamespaceByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return this.BadRequestProblem("Namespace name cannot be empty");
            }
            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var namespaceDto = await _namespaceService.GetNamespaceByNameAsync(name, userId);
            if (namespaceDto == null)
            {
                return this.NotFoundProblem($"Namespace '{name}' not found");
            }

            return Ok(namespaceDto);
        }

        [HttpPost]
        [AuthRequired]
        public async Task<ActionResult<NamespaceDto>> CreateNamespace([FromBody] CreateNamespaceDto createNamespaceDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var namespaceDto = await _namespaceService.CreateNamespaceAsync(createNamespaceDto, userId);

            return CreatedAtAction(nameof(GetNamespace), new { namespaceId = namespaceDto.Id }, namespaceDto);
        }

        [HttpPut]
        [AuthRequired]
        [Write]
        public async Task<ActionResult<NamespaceDto>> UpdateNamespace([FromBody] UpdateNamespaceDto updateNamespaceDto)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var namespaceDto = await _namespaceService.UpdateNamespaceAsync(namespaceId, updateNamespaceDto);
            return Ok(namespaceDto);
        }

        [HttpDelete]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> DeleteNamespace()
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (namespaceId <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            await _namespaceService.DeleteNamespaceAsync(namespaceId);
            return NoContent();
        }
    }
}