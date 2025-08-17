using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    current_version_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    value = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    key_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_versions_keys_key_id",
                        column: x => x.key_id,
                        principalTable: "keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_keys_current_version_id",
                table: "keys",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_keys_key_name",
                table: "keys",
                column: "key_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_versions_key_id",
                table: "versions",
                column: "key_id");

            migrationBuilder.AddForeignKey(
                name: "fk_keys_versions_current_version_id",
                table: "keys",
                column: "current_version_id",
                principalTable: "versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_keys_versions_current_version_id",
                table: "keys");

            migrationBuilder.DropTable(
                name: "versions");

            migrationBuilder.DropTable(
                name: "keys");
        }
    }
}
