using Microsoft.AspNetCore.Mvc;
using KeyNotFoundException = RevloDB.Services.KeyNotFoundException;
using RevloDB.DTOs;
using RevloDB.Services;
using RevloDB.Services.Interfaces;

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
        public async Task<ActionResult<string>> GetValue(string keyName)
        {
            var value = await _keyValueService.GetValueAsync(keyName);
            if (value == null)
            {
                return NotFound($"Key '{keyName}' not found");
            }

            return Ok(new { keyName, value });
        }

        [HttpPost]
        public async Task<ActionResult<KeyDto>> CreateKey([FromBody] CreateKeyDto createKeyDto)
        {
            try
            {
                var key = await _keyValueService.CreateKeyAsync(createKeyDto);
                return CreatedAtAction(nameof(GetKey), new { keyName = key.KeyName }, key);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{keyName}")]
        public async Task<ActionResult<KeyDto>> UpdateKey(string keyName, [FromBody] UpdateKeyDto updateKeyDto)
        {
            try
            {
                var key = await _keyValueService.UpdateKeyAsync(keyName, updateKeyDto);
                return Ok(key);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{keyName}")]
        public async Task<ActionResult> DeleteKey(string keyName)
        {
            try
            {
                await _keyValueService.DeleteKeyAsync(keyName);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{keyName}/history")]
        public async Task<ActionResult<IEnumerable<VersionDto>>> GetKeyHistory(string keyName)
        {
            try
            {
                var versions = await _keyValueService.GetKeyHistoryAsync(keyName);
                return Ok(versions);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{keyName}/version/{versionNumber}")]
        public async Task<ActionResult<string>> GetValueAtVersion(string keyName, int versionNumber)
        {
            var value = await _keyValueService.GetValueAtVersionAsync(keyName, versionNumber);
            if (value == null)
            {
                return NotFound($"Key '{keyName}' or version {versionNumber} not found");
            }

            return Ok(new { keyName, versionNumber, value });
        }
    }
}