using Microsoft.AspNetCore.Mvc;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;


namespace RevloDB.Controllers
{
    [Route("api/v1/user")]
    public class APIKeyController : ControllerBase
    {
        private readonly IAPIKeyService _apiKeyService;

        public APIKeyController(IAPIKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        [HttpPost("api-key")]
        [AuthRequired]
        public async Task<ActionResult<ApiKeyDto>> CreateApiKey([FromBody] CreateApiKeyDto createApiKeyDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }
            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var apiKey = await _apiKeyService.CreateApiKeyAsync(currentUserId, createApiKeyDto);
            return CreatedAtAction(nameof(GetApiKeys), new { }, apiKey);
        }

        [HttpGet("api-keys")]
        [AuthRequired]
        public async Task<ActionResult<IEnumerable<ApiKeyDto>>> GetApiKeys()
        {
            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);
            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(currentUserId);
            return Ok(apiKeys);
        }

        [HttpDelete("api-key/{apiKeyId}")]
        [AuthRequired]
        public async Task<IActionResult> DeleteApiKey(int apiKeyId)
        {
            var currentUserId = ControllerUtil.GetUserIdFromHTTPContext(HttpContext);

            await _apiKeyService.DeleteApiKeyAsync(currentUserId, apiKeyId);
            return NoContent();
        }
    }
}