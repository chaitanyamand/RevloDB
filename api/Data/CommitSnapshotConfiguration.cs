using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class CommitSnapshotConfiguration : IEntityTypeConfiguration<CommitSnapshot>
    {
        public void Configure(EntityTypeBuilder<CommitSnapshot> builder)
        {
            builder.ToTable("commit_snapshots");

            builder.Property(cs => cs.Id).HasColumnName("id");

            builder.Property(cs => cs.CommitId)
                .HasColumnName("commit_id");

            builder.Property(cs => cs.StateJson)
                .HasColumnName("state_json")
                .IsRequired();

            // Indexes
            builder.HasIndex(cs => cs.CommitId)
                .IsUnique()
                .HasDatabaseName("ix_commit_snapshots_commit_id");

            // Relationships
            builder.HasOne(cs => cs.Commit)
                .WithOne(c => c.Snapshot)
                .HasForeignKey<CommitSnapshot>(cs => cs.CommitId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_commit_snapshots_commits_commit_id");
        }
    }
}
