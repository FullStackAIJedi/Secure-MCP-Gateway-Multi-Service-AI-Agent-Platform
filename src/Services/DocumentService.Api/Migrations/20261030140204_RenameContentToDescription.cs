using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentService.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameContentToDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Documents",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_Content",
                table: "Documents",
                newName: "IX_Documents_Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Documents",
                newName: "Content");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_Description",
                table: "Documents",
                newName: "IX_Documents_Content");
        }
    }
}
