using Microsoft.EntityFrameworkCore;
using RevloDB.Data.Configurations;
using RevloDB.Entities;

namespace RevloDB.Data
{
    public class RevloDbContext : DbContext
    {
        public RevloDbContext(DbContextOptions<RevloDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Namespace> Namespaces { get; set; }
        public DbSet<UserNamespace> UserNamespaces { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Commit> Commits { get; set; }
        public DbSet<CommitChange> CommitChanges { get; set; }
        public DbSet<UnstagedChange> UnstagedChanges { get; set; }
        public DbSet<CommitSnapshot> CommitSnapshots { get; set; }
        public DbSet<BranchState> BranchStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new NamespaceConfiguration());
            modelBuilder.ApplyConfiguration(new UserNamespaceConfiguration());
            modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());
            modelBuilder.ApplyConfiguration(new BranchConfiguration());
            modelBuilder.ApplyConfiguration(new CommitConfiguration());
            modelBuilder.ApplyConfiguration(new CommitChangeConfiguration());
            modelBuilder.ApplyConfiguration(new UnstagedChangeConfiguration());
            modelBuilder.ApplyConfiguration(new CommitSnapshotConfiguration());
            modelBuilder.ApplyConfiguration(new BranchStateConfiguration());
        }
    }
}