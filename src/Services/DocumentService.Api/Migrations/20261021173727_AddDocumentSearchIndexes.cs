using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Documents_CreatedAt\" ON \"Documents\" (\"CreatedAt\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Documents_Title\" ON \"Documents\" USING gin (\"Title\" gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Documents_Content\" ON \"Documents\" USING gin (\"Content\" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_Content\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_Title\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_CreatedAt\";");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
