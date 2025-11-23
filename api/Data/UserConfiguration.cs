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
                .HasMaxLength(255);


            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            builder.Property(u => u.PasswordSalt)
                .HasColumnName("password_salt")
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            builder.Property(u => u.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasFilter("is_deleted = FALSE")
                .HasDatabaseName("ix_users_username");

            builder.HasIndex(u => u.IsDeleted)
                .HasFilter("is_deleted = TRUE")
                .HasDatabaseName("ix_users_is_deleted_true");

            // Relationships
            builder.HasMany(u => u.CreatedNamespaces)
                .WithOne(n => n.CreatedByUser)
                .HasForeignKey(n => n.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_namespaces_users_created_by_user_id");

            builder.HasMany(u => u.UserNamespaces)
                .WithOne(un => un.User)
                .HasForeignKey(un => un.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_namespaces_users_user_id");

            builder.HasMany(u => u.ApiKeys)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_api_keys_users_user_id");
        }
    }
}