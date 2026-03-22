using Microsoft.AspNetCore.Mvc;
using RevloDB.DTOs;
using RevloDB.Filters;
using RevloDB.Services.Interfaces;

namespace RevloDB.Controllers
{
    [ApiController]
    [Route("api/v1/namespaces/{nsId}/branches/{branchName}/keys")]
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
        public async Task<ActionResult<KeyValueListDto>> GetAllKeys(int nsId, string branchName)
        {
            var result = await _keyValueService.GetAllKeysAsync(branchName, nsId);
            return Ok(result);
        }

        [HttpGet("unstaged")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<IEnumerable<KeyValueDto>>> GetUnstagedChanges(int nsId, string branchName)
        {
            var result = await _keyValueService.GetUnstagedChangesAsync(branchName, nsId);
            return Ok(result);
        }

        [HttpGet("{keyName}")]
        [AuthRequired]
        [Read]
        public async Task<ActionResult<KeyValueDto>> GetKey(int nsId, string branchName, string keyName)
        {
            var result = await _keyValueService.GetKeyAsync(branchName, keyName, nsId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPut("{keyName}")]
        [AuthRequired]
        [Write]
        public async Task<ActionResult> SetKey(int nsId, string branchName, string keyName, [FromBody] SetKeyDto dto)
        {
            await _keyValueService.SetKeyAsync(branchName, keyName, dto.Value, nsId);
            return Ok();
        }

        [HttpDelete("{keyName}")]
        [AuthRequired]
        [Write]
        public async Task<ActionResult> DeleteKey(int nsId, string branchName, string keyName)
        {
            await _keyValueService.DeleteKeyAsync(branchName, keyName, nsId);
            return Ok();
        }
    }
}
