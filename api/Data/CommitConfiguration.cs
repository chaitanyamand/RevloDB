using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class CommitConfiguration : IEntityTypeConfiguration<Commit>
    {
        public void Configure(EntityTypeBuilder<Commit> builder)
        {
            builder.ToTable("commits");

            builder.Property(c => c.Id).HasColumnName("id");

            builder.Property(c => c.Hash)
                .HasColumnName("hash")
                .IsRequired();

            builder.Property(c => c.Message)
                .HasColumnName("message");

            builder.Property(c => c.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("NOW()");

            builder.Property(c => c.AuthorUserId)
                .HasColumnName("author_user_id");

            builder.Property(c => c.NamespaceId)
                .HasColumnName("namespace_id");

            builder.Property(c => c.Generation)
                .HasColumnName("generation");

            builder.Property(c => c.ParentCommitId)
                .HasColumnName("parent_commit_id");

            builder.Property(c => c.MergeParentCommitId)
                .HasColumnName("merge_parent_commit_id");

            // Indexes
            builder.HasIndex(c => new { c.Hash, c.NamespaceId })
                .IsUnique()
                .HasDatabaseName("ix_commits_hash_namespace_id");

            builder.HasIndex(c => c.NamespaceId)
                .HasDatabaseName("ix_commits_namespace_id");

            builder.HasIndex(c => c.ParentCommitId)
                .HasDatabaseName("ix_commits_parent_commit_id");

            builder.HasIndex(c => c.MergeParentCommitId)
                .HasFilter("merge_parent_commit_id IS NOT NULL")
                .HasDatabaseName("ix_commits_merge_parent_commit_id");

            builder.HasIndex(c => new { c.NamespaceId, c.Generation })
                .HasDatabaseName("ix_commits_generation");

            // Relationships
            builder.HasOne(c => c.AuthorUser)
                .WithMany(u => u.AuthoredCommits)
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_commits_users_author_user_id");

            builder.HasOne(c => c.ParentCommit)
                .WithMany()
                .HasForeignKey(c => c.ParentCommitId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_commits_commits_parent_commit_id");

            builder.HasOne(c => c.MergeParentCommit)
                .WithMany()
                .HasForeignKey(c => c.MergeParentCommitId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_commits_commits_merge_parent_commit_id");

            builder.HasOne(c => c.Namespace)
                .WithMany(n => n.Commits)
                .HasForeignKey(c => c.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_commits_namespaces_namespace_id");
        }
    }
}
