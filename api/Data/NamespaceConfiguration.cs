using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class NamespaceConfiguration : IEntityTypeConfiguration<Namespace>
    {
        public void Configure(EntityTypeBuilder<Namespace> builder)
        {
            builder.ToTable("namespaces");

            builder.Property(n => n.Id).HasColumnName("id");

            // Properties
            builder.Property(n => n.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(n => n.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            builder.Property(n => n.CreatedByUserId)
                .HasColumnName("created_by_user_id");

            builder.Property(n => n.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(n => new { n.Name, n.CreatedByUserId })
                .IsUnique()
                .HasDatabaseName("ix_namespaces_name_created_by_user_id");

            builder.HasIndex(n => n.CreatedByUserId)
                .HasDatabaseName("ix_namespaces_created_by_user_id");
        }
    }
}