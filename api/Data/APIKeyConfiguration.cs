using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.ToTable("api_keys");

            builder.Property(a => a.Id).HasColumnName("id");

            // Properties
            builder.Property(a => a.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(a => a.NamespaceId)
                .HasColumnName("namespace_id")
                .IsRequired();

            builder.Property(a => a.KeyValue)
                .HasColumnName("key_value")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(a => a.Role)
                .HasColumnName("role")
                .HasConversion<int>(); // Store enum as integer

            builder.Property(a => a.Description)
                .HasColumnName("description")
                .HasMaxLength(255);

            builder.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            builder.Property(a => a.ExpiresAt)
                .HasColumnName("expires_at");

            builder.Property(a => a.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(a => a.KeyValue)
                .IsUnique()
                .HasDatabaseName("ix_api_keys_key_value");

            builder.HasIndex(a => new { a.UserId, a.IsDeleted })
                .HasDatabaseName("ix_api_keys_user_id_is_deleted");

            builder.HasIndex(a => new { a.NamespaceId, a.IsDeleted })
                .HasDatabaseName("ix_api_keys_namespace_id_is_deleted");

            builder.HasIndex(a => new { a.ExpiresAt, a.IsDeleted })
                .HasFilter("expires_at IS NOT NULL")
                .HasDatabaseName("ix_api_keys_expires_at_is_deleted");

            builder.HasIndex(a => a.IsDeleted)
                .HasFilter("is_deleted = TRUE")
                .HasDatabaseName("ix_api_keys_is_deleted_true");

            // Relationships
            builder.HasOne(a => a.User)
                .WithMany(u => u.ApiKeys)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_api_keys_users_user_id");


            builder.HasOne(a => a.Namespace)
                .WithMany(n => n.ApiKeys)
                .HasForeignKey(a => a.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_api_keys_namespaces_namespace_id");
        }
    }
}