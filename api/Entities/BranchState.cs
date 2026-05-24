using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class BranchState
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string KeyName { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public int LastModifiedCommitId { get; set; }
    }
}
