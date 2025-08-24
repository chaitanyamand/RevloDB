using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class KeyDto
    {
        public int Id { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public int? CurrentVersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateKeyDto
    {
        [Required(ErrorMessage = "Key name cannot be empty")]
        [MinLength(3, ErrorMessage = "Key name must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Key name cannot exceed 100 characters")]
        public string KeyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Value is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Value cannot be empty")]
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateKeyDto
    {
        [Required(ErrorMessage = "Value is required for an update")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Value cannot be empty")]
        public string Value { get; set; } = string.Empty;
    }

    public class RevertKeyDto
    {
        [Required(ErrorMessage = "Key name cannot be empty")]
        [MinLength(3, ErrorMessage = "Key name must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Key name cannot exceed 100 characters")]
        public string KeyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Version number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Version number must be a positive integer")]
        public int VersionNumber { get; set; } = 0;
    }
}