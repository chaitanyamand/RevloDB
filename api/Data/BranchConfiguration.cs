using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("branches");

            builder.Property(b => b.Id).HasColumnName("id");

            builder.Property(b => b.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(b => b.NamespaceId)
                .HasColumnName("namespace_id");

            builder.Property(b => b.HeadCommitId)
                .HasColumnName("head_commit_id");

            builder.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // Indexes
            builder.HasIndex(b => new { b.Name, b.NamespaceId })
                .IsUnique()
                .HasDatabaseName("ix_branches_name_namespace_id");

            builder.HasIndex(b => b.NamespaceId)
                .HasDatabaseName("ix_branches_namespace_id");

            builder.HasIndex(b => b.HeadCommitId)
                .HasDatabaseName("ix_branches_head_commit_id");

            // Relationships
            builder.HasOne(b => b.Namespace)
                .WithMany(n => n.Branches)
                .HasForeignKey(b => b.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_branches_namespaces_namespace_id");

            builder.HasOne(b => b.HeadCommit)
                .WithMany()
                .HasForeignKey(b => b.HeadCommitId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_branches_commits_head_commit_id");
        }
    }
}
