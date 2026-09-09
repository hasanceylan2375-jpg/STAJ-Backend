using Microsoft.EntityFrameworkCore;
using STAJ.Entities;

namespace STAJ.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Musteri> Musteriler { get; set; }
        public DbSet<Sirket> Sirketler { get; set; }
        public DbSet<Konut> Konutlar { get; set; }
        public DbSet<Arac> Araclar { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<WorkflowRequest> WorkflowRequests { get; set; }
        public DbSet<RagDocument> RagDocuments { get; set; }
        public DbSet<RagChunk> RagChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<Musteri>().HasIndex(x => x.TcKimlikNo).IsUnique();
            modelBuilder.Entity<RefreshToken>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<IdempotencyRecord>().HasIndex(x => x.Key).IsUnique();
            modelBuilder.Entity<WorkflowRequest>().HasIndex(x => new { x.Type, x.Status });
            modelBuilder.Entity<WorkflowRequest>().HasIndex(x => x.RequestedBy);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RagDocument>()
                .HasIndex(x => new { x.CompanyName, x.UploadedAtUtc });

            modelBuilder.Entity<RagDocument>()
                .HasMany(x => x.Chunks)
                .WithOne(x => x.RagDocument)
                .HasForeignKey(x => x.RagDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RagChunk>()
                .HasIndex(x => new { x.RagDocumentId, x.ChunkIndex })
                .IsUnique();

            modelBuilder.Entity<RagChunk>()
                .HasIndex(x => x.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops")
                .HasStorageParameter("m", 16)
                .HasStorageParameter("ef_construction", 64);
        }
    }
}
