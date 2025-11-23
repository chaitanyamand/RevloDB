using System.ComponentModel.DataAnnotations;

namespace RevloDB.DTOs
{
    public class UserNamespaceDto
    {
        public int UserId { get; set; }
        public int NamespaceId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public NamespaceDto Namespace { get; set; } = null!;
    }

    public class GrantAccessDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int NamespaceId { get; set; }
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class RevokeAccessDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int NamespaceId { get; set; }
    }
}