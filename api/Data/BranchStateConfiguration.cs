using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class BranchStateConfiguration : IEntityTypeConfiguration<BranchState>
    {
        public void Configure(EntityTypeBuilder<BranchState> builder)
        {
            builder.ToTable("branch_states");

            builder.Property(bs => bs.Id).HasColumnName("id");

            builder.Property(bs => bs.BranchId)
                .HasColumnName("branch_id");

            builder.Property(bs => bs.KeyName)
                .HasColumnName("key_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(bs => bs.Value)
                .HasColumnName("value")
                .IsRequired();

            builder.Property(bs => bs.LastModifiedCommitId)
                .HasColumnName("last_modified_commit_id");

            // Indexes
            builder.HasIndex(bs => new { bs.BranchId, bs.KeyName })
                .IsUnique()
                .HasDatabaseName("ix_branch_states_branch_id_key_name");

            builder.HasIndex(bs => bs.BranchId)
                .HasDatabaseName("ix_branch_states_branch_id");

            // Relationships
            builder.HasOne(bs => bs.Branch)
                .WithMany(b => b.BranchStates)
                .HasForeignKey(bs => bs.BranchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_branch_states_branches_branch_id");
        }
    }
}
