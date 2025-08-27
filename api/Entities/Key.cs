using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class Key
    {
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string KeyName { get; set; } = string.Empty;
        public int? CurrentVersionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int NamespaceId { get; set; }
        public Version? CurrentVersion { get; set; }
        public ICollection<Version> Versions { get; set; } = new List<Version>();
        public Namespace Namespace { get; set; } = null!;
    }
}