using Microsoft.EntityFrameworkCore;
using Version = RevloDB.Entities.Version;
using Key = RevloDB.Entities.Key;

namespace RevloDB.Data
{
    public class RevloDbContext : DbContext
    {
        public RevloDbContext(DbContextOptions<RevloDbContext> options) : base(options)
        {
        }

        public DbSet<Key> Keys { get; set; }
        public DbSet<Version> Versions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Key>(entity =>
            {
                entity.ToTable("keys");

                entity.HasIndex(k => k.KeyName)
                      .IsUnique()
                      .HasDatabaseName("ix_keys_key_name");

                entity.Property(k => k.Id).HasColumnName("id");
                entity.Property(k => k.KeyName).HasColumnName("key_name");
                entity.Property(k => k.CurrentVersionId).HasColumnName("current_version_id");
                entity.Property(k => k.CreatedAt).HasColumnName("created_at");

                entity.Property(k => k.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });

            modelBuilder.Entity<Version>(entity =>
            {
                entity.ToTable("versions");

                entity.Property(v => v.Id).HasColumnName("id");
                entity.Property(v => v.Value).HasColumnName("value");
                entity.Property(v => v.Timestamp).HasColumnName("timestamp");
                entity.Property(v => v.VersionNumber).HasColumnName("version_number");
                entity.Property(v => v.KeyId).HasColumnName("key_id");

                entity.Property(v => v.Timestamp)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(v => v.Key)
                      .WithMany(k => k.Versions)
                      .HasForeignKey(v => v.KeyId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("fk_versions_keys_key_id");
            });

            modelBuilder.Entity<Key>()
                .HasOne(k => k.CurrentVersion)
                .WithMany()
                .HasForeignKey(k => k.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_keys_versions_current_version_id");
        }
    }
}