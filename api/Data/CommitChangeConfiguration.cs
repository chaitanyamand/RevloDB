using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class CommitChangeConfiguration : IEntityTypeConfiguration<CommitChange>
    {
        public void Configure(EntityTypeBuilder<CommitChange> builder)
        {
            builder.ToTable("commit_changes");

            builder.Property(cc => cc.Id).HasColumnName("id");

            builder.Property(cc => cc.CommitId)
                .HasColumnName("commit_id");

            builder.Property(cc => cc.KeyName)
                .HasColumnName("key_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(cc => cc.Value)
                .HasColumnName("value");

            builder.Property(cc => cc.Action)
                .HasColumnName("action")
                .HasConversion<string>();

            // Indexes
            builder.HasIndex(cc => cc.CommitId)
                .HasDatabaseName("ix_commit_changes_commit_id");

            builder.HasIndex(cc => new { cc.CommitId, cc.KeyName })
                .HasDatabaseName("ix_commit_changes_commit_id_key_name");

            // Relationships
            builder.HasOne(cc => cc.Commit)
                .WithMany(c => c.Changes)
                .HasForeignKey(cc => cc.CommitId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_commit_changes_commits_commit_id");
        }
    }
}
