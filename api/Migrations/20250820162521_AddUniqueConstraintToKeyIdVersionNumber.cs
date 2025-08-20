using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToKeyIdVersionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_versions_key_id",
                table: "versions");

            migrationBuilder.CreateIndex(
                name: "ix_versions_key_id_version_number",
                table: "versions",
                columns: new[] { "key_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_versions_key_id_version_number",
                table: "versions");

            migrationBuilder.CreateIndex(
                name: "IX_versions_key_id",
                table: "versions",
                column: "key_id");
        }
    }
}
