using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class KeyValueDto
    {
        public string KeyName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class KeyValueListDto
    {
        public List<KeyValueDto> Keys { get; set; } = new List<KeyValueDto>();
        public string BranchName { get; set; } = string.Empty;
        public string? HeadCommitHash { get; set; }
    }

    public class SetKeyDto
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
