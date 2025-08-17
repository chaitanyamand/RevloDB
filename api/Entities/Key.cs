using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RevloDB.Entities
{
    public class Key
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string KeyName { get; set; } = string.Empty;
        public int? CurrentVersionId { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [ForeignKey("CurrentVersionId")]
        public Version? CurrentVersion { get; set; }
        public ICollection<Version> Versions { get; set; } = new List<Version>();
    }
}