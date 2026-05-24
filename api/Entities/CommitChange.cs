using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class CommitChange : IChange
    {
        public int Id { get; set; }

        public int CommitId { get; set; }
        public Commit Commit { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string KeyName { get; set; } = string.Empty;

        public string? Value { get; set; }

        public ChangeAction Action { get; set; }
    }
}
