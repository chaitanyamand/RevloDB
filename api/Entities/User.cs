using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [StringLength(255)]
        public string? ApiKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<Namespace> CreatedNamespaces { get; set; } = new List<Namespace>();
        public ICollection<UserNamespace> UserNamespaces { get; set; } = new List<UserNamespace>();
    }
}