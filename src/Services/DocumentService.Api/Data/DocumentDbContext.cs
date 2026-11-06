namespace DocumentService.Api.Data
{
    using Microsoft.EntityFrameworkCore;

    using DocumentService.Api.Entities;

    public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
    {
        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Embedding).HasColumnType("vector(768)");

                entity.HasIndex(e => e.CreatedAt);

                entity.HasIndex(e => e.Title)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");

                entity.HasIndex(e => e.Description)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");

                entity.Property(e => e.OwnerSubjectId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.OwnerSubjectId)
                    .HasDatabaseName("IX_Documents_OwnerSubjectId");
            });
        }
    }
}
