using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HeadCommitHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBranchDto
    {
        [Required(ErrorMessage = "Branch name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Branch name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        public string SourceBranchName { get; set; } = "main";
    }
}
