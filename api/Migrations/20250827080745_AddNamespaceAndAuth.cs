using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddNamespaceAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_keys_key_name",
                table: "keys");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                table: "keys",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<int>(
                name: "namespace_id",
                table: "keys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    api_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "namespaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_namespaces", x => x.id);
                    table.ForeignKey(
                        name: "fk_namespaces_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_namespaces",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    namespace_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_namespaces", x => new { x.user_id, x.namespace_id });
                    table.ForeignKey(
                        name: "fk_user_namespaces_namespaces_namespace_id",
                        column: x => x.namespace_id,
                        principalTable: "namespaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_namespaces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_versions_key_id",
                table: "versions",
                column: "key_id");

            migrationBuilder.CreateIndex(
                name: "ix_keys_key_name_namespace_id",
                table: "keys",
                columns: new[] { "key_name", "namespace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_keys_namespace_id",
                table: "keys",
                column: "namespace_id");

            migrationBuilder.CreateIndex(
                name: "ix_namespaces_created_by_user_id",
                table: "namespaces",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_namespaces_name_created_by_user_id",
                table: "namespaces",
                columns: new[] { "name", "created_by_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_namespaces_namespace_id",
                table: "user_namespaces",
                column: "namespace_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_namespaces_user_id",
                table: "user_namespaces",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_api_key",
                table: "users",
                column: "api_key",
                unique: true,
                filter: "api_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_keys_namespaces_namespace_id",
                table: "keys",
                column: "namespace_id",
                principalTable: "namespaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_keys_namespaces_namespace_id",
                table: "keys");

            migrationBuilder.DropTable(
                name: "user_namespaces");

            migrationBuilder.DropTable(
                name: "namespaces");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "ix_versions_key_id",
                table: "versions");

            migrationBuilder.DropIndex(
                name: "ix_keys_key_name_namespace_id",
                table: "keys");

            migrationBuilder.DropIndex(
                name: "ix_keys_namespace_id",
                table: "keys");

            migrationBuilder.DropColumn(
                name: "namespace_id",
                table: "keys");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                table: "keys",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_keys_key_name",
                table: "keys",
                column: "key_name",
                unique: true);
        }
    }
}
