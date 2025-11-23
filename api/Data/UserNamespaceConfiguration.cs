using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class UserNamespaceConfiguration : IEntityTypeConfiguration<UserNamespace>
    {
        public void Configure(EntityTypeBuilder<UserNamespace> builder)
        {
            builder.ToTable("user_namespaces");

            builder.HasKey(un => new { un.UserId, un.NamespaceId });

            // Properties
            builder.Property(un => un.UserId)
                .HasColumnName("user_id");

            builder.Property(un => un.NamespaceId)
                .HasColumnName("namespace_id");

            builder.Property(un => un.Role)
                .HasColumnName("role")
                .HasConversion<int>(); // Store enum as integer

            builder.Property(un => un.GrantedAt)
                .HasColumnName("granted_at")
                .HasDefaultValueSql("NOW()");

            // Indexes
            builder.HasIndex(un => un.NamespaceId)
                .HasDatabaseName("ix_user_namespaces_namespace_id");

            // Relationships
            builder.HasOne(un => un.User)
                .WithMany(u => u.UserNamespaces)
                .HasForeignKey(un => un.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_namespaces_users_user_id");

            builder.HasOne(un => un.Namespace)
                .WithMany(n => n.UserNamespaces)
                .HasForeignKey(un => un.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_namespaces_namespaces_namespace_id");
        }
    }
}