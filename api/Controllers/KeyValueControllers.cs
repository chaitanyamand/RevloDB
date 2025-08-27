using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Services.Interfaces;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Utils;

namespace RevloDB.Controllers
{
    [Route("api/v1/[controller]")]
    public class KeyValueController : ControllerBase
    {
        private readonly IKeyValueService _keyValueService;

        public KeyValueController(IKeyValueService keyValueService)
        {
            _keyValueService = keyValueService;
        }

        [HttpGet]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<IEnumerable<KeyDto>>> GetAllKeys()
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var keys = await _keyValueService.GetAllKeysAsync(namespaceId);
            return Ok(keys);
        }

        [HttpGet("{keyName}")]
        public async Task<ActionResult<KeyDto>> GetKey(string keyName)
        {
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }

            var key = await _keyValueService.GetKeyAsync(keyName, namespaceId!);
            if (key == null)
            {
                return this.NotFoundProblem($"Key '{keyName}' not found");
            }

            return Ok(key);
        }

        [HttpGet("{keyName}/value")]
        public async Task<ActionResult<KeyValueDto>> GetValue(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var value = await _keyValueService.GetValueAsync(keyName, namespaceId);
            if (value == null)
            {
                return this.NotFoundProblem($"Key '{keyName}' not found");
            }

            return Ok(new KeyValueDto { KeyName = keyName, Value = value });
        }

        [HttpPost]
        public async Task<ActionResult<KeyDto>> CreateKey([FromBody] CreateKeyDto createKeyDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var key = await _keyValueService.CreateKeyAsync(createKeyDto, namespaceId);
            return CreatedAtAction(nameof(GetKey), new { keyName = key.KeyName }, key);
        }

        [HttpPut("{keyName}")]
        public async Task<ActionResult<KeyDto>> UpdateKey(string keyName, [FromBody] UpdateKeyDto updateKeyDto)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }

            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var key = await _keyValueService.UpdateKeyAsync(keyName, updateKeyDto, namespaceId);
            return Ok(key);
        }

        [HttpDelete("{keyName}")]
        public async Task<IActionResult> DeleteKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            await _keyValueService.DeleteKeyAsync(keyName, namespaceId);
            return NoContent();
        }

        [HttpPost("{keyName}/restore")]
        public async Task<IActionResult> RestoreKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            await _keyValueService.RestoreKeyAsync(keyName, namespaceId);
            return NoContent();
        }

        [HttpGet("{keyName}/history")]
        public async Task<ActionResult<IEnumerable<VersionDto>>> GetKeyHistory(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var versions = await _keyValueService.GetKeyHistoryAsync(keyName, namespaceId);
            return Ok(versions);
        }

        [HttpPost("revert")]
        public async Task<ActionResult<KeyDto>> RevertToVersion([FromBody] RevertKeyDto revertKeyDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ModelValidationProblem(ModelState);
            }
            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var key = await _keyValueService.RevertKeyAsync(revertKeyDto, namespaceId);
            return Ok(key);
        }

        [HttpGet("{keyName}/version/{versionNumber}")]
        public async Task<ActionResult<KeyVersionValueDto>> GetValueAtVersion(string keyName, int versionNumber)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return this.BadRequestProblem("Key name cannot be empty");
            }

            if (versionNumber <= 0)
            {
                return this.BadRequestProblem("Version number must be a positive integer");
            }

            var namespaceId = ControllerUtil.GetNameSpaceIdFromHTTPContext(HttpContext);
            var value = await _keyValueService.GetValueAtVersionAsync(keyName, versionNumber, namespaceId);
            if (value == null)
            {
                return this.NotFoundProblem($"Key '{keyName}' or version {versionNumber} not found");
            }

            return Ok(new KeyVersionValueDto { KeyName = keyName, VersionNumber = versionNumber, Value = value });
        }
    }
}