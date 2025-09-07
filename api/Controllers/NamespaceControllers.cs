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

        [HttpGet("{id}")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<NamespaceDto>> GetNamespace(int id)
        {
            if (id <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            var namespaceDto = await _namespaceService.GetNamespaceByIdAsync(id);
            if (namespaceDto == null)
            {
                return this.NotFoundProblem($"Namespace with id {id} not found");
            }

            return Ok(namespaceDto);
        }

        [HttpGet("by-name/{name}")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<NamespaceDto>> GetNamespaceByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return this.BadRequestProblem("Namespace name cannot be empty");
            }

            var namespaceDto = await _namespaceService.GetNamespaceByNameAsync(name);
            if (namespaceDto == null)
            {
                return this.NotFoundProblem($"Namespace '{name}' not found");
            }

            return Ok(namespaceDto);
        }

        [HttpPost]
        [AuthRequired]
        [Write]
        public async Task<ActionResult<NamespaceDto>> CreateNamespace([FromBody] CreateNamespaceDto createNamespaceDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var userId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var namespaceDto = await _namespaceService.CreateNamespaceAsync(createNamespaceDto, userId);

            return CreatedAtAction(nameof(GetNamespace), new { id = namespaceDto.Id }, namespaceDto);
        }

        [HttpPut("{id}")]
        [AuthRequired]
        [Write]
        public async Task<ActionResult<NamespaceDto>> UpdateNamespace(int id, [FromBody] UpdateNamespaceDto updateNamespaceDto)
        {
            if (id <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }

            var namespaceDto = await _namespaceService.UpdateNamespaceAsync(id, updateNamespaceDto);
            return Ok(namespaceDto);
        }

        [HttpDelete("{id}")]
        [AuthRequired]
        [Write]
        public async Task<IActionResult> DeleteNamespace(int id)
        {
            if (id <= 0)
            {
                return this.BadRequestProblem("Namespace ID must be a positive integer");
            }

            await _namespaceService.DeleteNamespaceAsync(id);
            return NoContent();
        }
    }
}