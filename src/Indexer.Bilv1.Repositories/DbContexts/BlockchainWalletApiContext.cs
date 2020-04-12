using Indexer.Bilv1.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Bilv1.Repositories.DbContexts
{
    public class IndexerBilV1Context : DbContext
    {
        public IndexerBilV1Context(DbContextOptions<IndexerBilV1Context> options) :
            base(options)
        {
        }

        public DbSet<WalletEntity> Wallets { get; set; }
        public DbSet<EnrolledBalanceEntity> EnrolledBalances { get; set; }
        public DbSet<OperationEntity> Operations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(PostgresBilV1RepositoryConfiguration.SchemaName);
            
            modelBuilder.Entity<WalletEntity>()
                .HasKey(c => new { c.BlockchainId, c.NetworkId, c.Id});

            modelBuilder
                .Entity<WalletEntity>()
                .HasIndex(c => new { c.BlockchainId, c.NetworkId, c.WalletAddress})
                .IsUnique(true)
                .HasName("IX_BlockchainId_NetworkId_WalletAddress");

            modelBuilder.Entity<EnrolledBalanceEntity>()
                .HasKey(c => new { c.BlockchianId, c.BlockchainAssetId, c.WalletAddress });

            modelBuilder.Entity<OperationEntity>()
                .HasKey(c => new {c.BlockchianId, c.BlockchainAssetId, c.WalletAddress, c.OperationId});

            base.OnModelCreating(modelBuilder);
        }
    }
}
