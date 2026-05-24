using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class CommitSnapshot
    {
        public int Id { get; set; }

        public int CommitId { get; set; }
        public Commit Commit { get; set; } = null!;

        [Required]
        public string StateJson { get; set; } = string.Empty;
    }
}
