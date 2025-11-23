using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class ApiKey
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int NamespaceId { get; set; }

        [Required]
        [StringLength(128)]
        public string KeyValue { get; set; } = string.Empty;

        [Required]
        public NamespaceRole Role { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public User User { get; set; } = null!;
        public Namespace Namespace { get; set; } = null!;
    }
}
