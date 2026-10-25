namespace TaskService.Api.Data
{
    using Microsoft.EntityFrameworkCore;

    using TaskService.Api.Entities;

    public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
    {
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.OwnerSubjectId);
            });
        }
    } 
}
