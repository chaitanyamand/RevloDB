using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class CommitDto
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Author { get; set; } = string.Empty;
        public int Generation { get; set; }
        public List<CommitChangeDto> Changes { get; set; } = new List<CommitChangeDto>();
    }

    public class CreateCommitDto
    {
        [Required(ErrorMessage = "Commit message is required")]
        public string Message { get; set; } = string.Empty;
    }

    public class CommitChangeDto
    {
        public string KeyName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
