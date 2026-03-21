using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class Commit
    {
        public int Id { get; set; }

        [Required]
        public string Hash { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public int AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = null!;

        public int NamespaceId { get; set; }
        public Namespace Namespace { get; set; } = null!;

        public int Generation { get; set; }

        public int? ParentCommitId { get; set; }
        public Commit? ParentCommit { get; set; }

        public int? MergeParentCommitId { get; set; }
        public Commit? MergeParentCommit { get; set; }

        public ICollection<CommitChange> Changes { get; set; } = new List<CommitChange>();
        public CommitSnapshot? Snapshot { get; set; }
    }
}
