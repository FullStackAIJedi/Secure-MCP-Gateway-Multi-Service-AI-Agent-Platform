using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerSubjectIdToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerSubjectId",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerSubjectId",
                table: "Documents",
                column: "OwnerSubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_OwnerSubjectId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OwnerSubjectId",
                table: "Documents");
        }
    }
}
