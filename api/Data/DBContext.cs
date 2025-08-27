using Microsoft.EntityFrameworkCore;
using RevloDB.Data.Configurations;
using RevloDB.Entities;
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

        public DbSet<User> Users { get; set; }
        public DbSet<Namespace> Namespaces { get; set; }
        public DbSet<UserNamespace> UserNamespaces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new NamespaceConfiguration());
            modelBuilder.ApplyConfiguration(new UserNamespaceConfiguration());
            modelBuilder.ApplyConfiguration(new KeyConfiguration());
            modelBuilder.ApplyConfiguration(new VersionConfiguration());
        }
    }
}