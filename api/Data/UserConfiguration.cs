using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.Property(u => u.Id).HasColumnName("id");

            // Properties
            builder.Property(u => u.Username)
                .HasColumnName("username")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            builder.Property(u => u.ApiKey)
                .HasColumnName("api_key")
                .HasMaxLength(255);

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            builder.Property(u => u.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("ix_users_username");

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            builder.HasIndex(u => u.ApiKey)
                .IsUnique()
                .HasFilter("api_key IS NOT NULL")
                .HasDatabaseName("ix_users_api_key");

            // Relationships
            builder.HasMany(u => u.CreatedNamespaces)
                .WithOne(n => n.CreatedByUser)
                .HasForeignKey(n => n.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_namespaces_users_created_by_user_id");
        }
    }
}