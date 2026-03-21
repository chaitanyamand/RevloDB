using System.ComponentModel.DataAnnotations;

namespace RevloDB.Entities
{
    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int NamespaceId { get; set; }
        public Namespace Namespace { get; set; } = null!;

        public int? HeadCommitId { get; set; }
        public Commit? HeadCommit { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<UnstagedChange> UnstagedChanges { get; set; } = new List<UnstagedChange>();
        public ICollection<BranchState> BranchStates { get; set; } = new List<BranchState>();
    }
}
