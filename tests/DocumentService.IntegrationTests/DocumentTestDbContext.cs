namespace DocumentService.IntegrationTests
{
	using Microsoft.EntityFrameworkCore;

	public sealed class DocumentTestDbContext(DbContextOptions<DocumentTestDbContext> options)
		: DbContext(options)
	{
		public DbSet<TestDocument> Documents => Set<TestDocument>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TestDocument>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
				entity.Property(e => e.Content).IsRequired();
			});
		}
	}

	public sealed class TestDocument
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	}
}