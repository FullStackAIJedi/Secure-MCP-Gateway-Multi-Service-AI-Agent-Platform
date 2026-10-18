namespace DocumentService.Api.Data
{ 
    using Microsoft.EntityFrameworkCore;  
 
    public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
    {
        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Embedding).HasColumnType("vector(768)");
            });
        }
    }

    public sealed class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Pgvector.Vector? Embedding { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
