using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class UnstagedChange : IChange
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string KeyName { get; set; } = string.Empty;

        public string? Value { get; set; }
        public ChangeAction Action { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
