using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class UnstagedChangeConfiguration : IEntityTypeConfiguration<UnstagedChange>
    {
        public void Configure(EntityTypeBuilder<UnstagedChange> builder)
        {
            builder.ToTable("unstaged_changes");

            builder.Property(uc => uc.Id).HasColumnName("id");

            builder.Property(uc => uc.BranchId)
                .HasColumnName("branch_id");

            builder.Property(uc => uc.KeyName)
                .HasColumnName("key_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(uc => uc.Value)
                .HasColumnName("value");

            builder.Property(uc => uc.Action)
                .HasColumnName("action")
                .HasConversion<string>();

            builder.Property(uc => uc.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            // Indexes
            builder.HasIndex(uc => new { uc.BranchId, uc.KeyName })
                .IsUnique()
                .HasDatabaseName("ix_unstaged_changes_branch_id_key_name");

            builder.HasIndex(uc => uc.BranchId)
                .HasDatabaseName("ix_unstaged_changes_branch_id");

            // Relationships
            builder.HasOne(uc => uc.Branch)
                .WithMany(b => b.UnstagedChanges)
                .HasForeignKey(uc => uc.BranchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_unstaged_changes_branches_branch_id");
        }
    }
}
