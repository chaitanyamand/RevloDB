using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class Namespace
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(500)]
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public User CreatedByUser { get; set; } = null!;
        public ICollection<Key> Keys { get; set; } = new List<Key>();
        public ICollection<UserNamespace> UserNamespaces { get; set; } = new List<UserNamespace>();
        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    }
}