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
                .HasFilter("is_deleted = FALSE")
                .HasDatabaseName("ix_namespaces_name_created_by_user_id");

            builder.HasIndex(n => n.IsDeleted)
                .HasFilter("is_deleted = TRUE")
                .HasDatabaseName("ix_namespace_is_deleted_true");

            builder.HasIndex(n => n.CreatedByUserId)
                .HasDatabaseName("ix_namespaces_created_by_user_id");

            //Relationships
            builder.HasOne(n => n.CreatedByUser)
                .WithMany(u => u.CreatedNamespaces)
                .HasForeignKey(n => n.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_namespaces_users_created_by_user_id");

            builder.HasMany(n => n.UserNamespaces)
                .WithOne(un => un.Namespace)
                .HasForeignKey(un => un.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_namespaces_namespaces_namespace_id");

            builder.HasMany(n => n.ApiKeys)
                .WithOne(a => a.Namespace)
                .HasForeignKey(a => a.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_api_keys_namespaces_namespace_id");

            builder.HasMany(n => n.Keys)
                .WithOne(k => k.Namespace)
                .HasForeignKey(k => k.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_keys_namespaces_namespace_id");
        }
    }
}