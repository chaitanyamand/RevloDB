using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Services.Interfaces;
using RevloDB.Services;

namespace RevloDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeyValueController : ControllerBase
    {
        private readonly IKeyValueService _keyValueService;

        public KeyValueController(IKeyValueService keyValueService)
        {
            _keyValueService = keyValueService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KeyDto>>> GetAllKeys()
        {
            var keys = await _keyValueService.GetAllKeysAsync();
            return Ok(keys);
        }

        [HttpGet("{keyName}")]
        public async Task<ActionResult<KeyDto>> GetKey(string keyName)
        {
            var key = await _keyValueService.GetKeyAsync(keyName);
            if (key == null)
            {
                return NotFound($"Key '{keyName}' not found");
            }

            return Ok(key);
        }

        [HttpGet("{keyName}/value")]
        public async Task<ActionResult<KeyValueDto>> GetValue(string keyName)
        {
            var value = await _keyValueService.GetValueAsync(keyName);
            if (value == null)
            {
                return NotFound($"Key '{keyName}' not found");
            }

            return Ok(new KeyValueDto { KeyName = keyName, Value = value });
        }

        [HttpPost]
        public async Task<ActionResult<KeyDto>> CreateKey([FromBody] CreateKeyDto createKeyDto)
        {
            var key = await _keyValueService.CreateKeyAsync(createKeyDto);
            return CreatedAtAction(nameof(GetKey), new { keyName = key.KeyName }, key);
        }

        [HttpPut("{keyName}")]
        public async Task<ActionResult<KeyDto>> UpdateKey(string keyName, [FromBody] UpdateKeyDto updateKeyDto)
        {
            var key = await _keyValueService.UpdateKeyAsync(keyName, updateKeyDto);
            return Ok(key);
        }

        [HttpDelete("{keyName}")]
        public async Task<IActionResult> DeleteKey(string keyName)
        {
            await _keyValueService.DeleteKeyAsync(keyName);
            return NoContent();
        }

        [HttpGet("{keyName}/history")]
        public async Task<ActionResult<IEnumerable<VersionDto>>> GetKeyHistory(string keyName)
        {
            var versions = await _keyValueService.GetKeyHistoryAsync(keyName);
            return Ok(versions);
        }

        [HttpGet("{keyName}/version/{versionNumber}")]
        public async Task<ActionResult<KeyVersionValueDto>> GetValueAtVersion(string keyName, int versionNumber)
        {
            var value = await _keyValueService.GetValueAtVersionAsync(keyName, versionNumber);
            if (value == null)
            {
                return NotFound($"Key '{keyName}' or version {versionNumber} not found");
            }

            return Ok(new KeyVersionValueDto { KeyName = keyName, VersionNumber = versionNumber, Value = value });
        }
    }
}