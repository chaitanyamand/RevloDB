using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RevloDB.Entities
{
    public class Version
    {
        public int Id { get; set; }
        [Required]
        public string Value { get; set; } = string.Empty;
        [Required]
        public DateTime Timestamp { get; set; }
        [Required]
        public int VersionNumber { get; set; }
        [Required]
        public int KeyId { get; set; }
        public Key Key { get; set; } = null!;
    }
}