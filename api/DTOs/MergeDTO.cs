using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class MergeRequestDto
    {
        [Required]
        public string SourceBranchName { get; set; } = string.Empty;
        public string? Message { get; set; }
    }

    public class MergeConflictDto
    {
        public string Key { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string? IncomingValue { get; set; }
    }

    public class MergeResultDto
    {
        public bool Success { get; set; }
        public bool IsNoOp { get; set; }
        public string? MergeCommitHash { get; set; }
        public List<MergeConflictDto>? Conflicts { get; set; }
    }
}
