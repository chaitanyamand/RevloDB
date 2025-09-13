using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AuthAndNamespaceCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_namespaces_users_created_by_user_id",
                table: "namespaces");

            migrationBuilder.DropIndex(
                name: "ix_users_api_key",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_user_namespaces_user_id",
                table: "user_namespaces");

            migrationBuilder.DropIndex(
                name: "ix_namespaces_name_created_by_user_id",
                table: "namespaces");

            migrationBuilder.DropIndex(
                name: "ix_keys_key_name_namespace_id",
                table: "keys");

            migrationBuilder.DropColumn(
                name: "api_key",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "password_salt",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "created_by_user_id",
                table: "namespaces",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    namespace_id = table.Column<int>(type: "integer", nullable: false),
                    key_value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_keys_namespaces_namespace_id",
                        column: x => x.namespace_id,
                        principalTable: "namespaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_is_deleted_true",
                table: "users",
                column: "is_deleted",
                filter: "is_deleted = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_namespace_is_deleted_true",
                table: "namespaces",
                column: "is_deleted",
                filter: "is_deleted = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_namespaces_name_created_by_user_id",
                table: "namespaces",
                columns: new[] { "name", "created_by_user_id" },
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_keys_unique_active_key_name_namespace_id",
                table: "keys",
                columns: new[] { "key_name", "namespace_id" },
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_expires_at_is_deleted",
                table: "api_keys",
                columns: new[] { "expires_at", "is_deleted" },
                filter: "expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_is_deleted_true",
                table: "api_keys",
                column: "is_deleted",
                filter: "is_deleted = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_key_value",
                table: "api_keys",
                column: "key_value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_namespace_id_is_deleted",
                table: "api_keys",
                columns: new[] { "namespace_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_user_id_is_deleted",
                table: "api_keys",
                columns: new[] { "user_id", "is_deleted" });

            migrationBuilder.AddForeignKey(
                name: "fk_namespaces_users_created_by_user_id",
                table: "namespaces",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_namespaces_users_created_by_user_id",
                table: "namespaces");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropIndex(
                name: "ix_users_is_deleted_true",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_namespace_is_deleted_true",
                table: "namespaces");

            migrationBuilder.DropIndex(
                name: "ix_namespaces_name_created_by_user_id",
                table: "namespaces");

            migrationBuilder.DropIndex(
                name: "ix_keys_unique_active_key_name_namespace_id",
                table: "keys");

            migrationBuilder.DropColumn(
                name: "password_salt",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "api_key",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "created_by_user_id",
                table: "namespaces",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_user_namespaces_user_id",
                table: "user_namespaces",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_namespaces_name_created_by_user_id",
                table: "namespaces",
                columns: new[] { "name", "created_by_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_keys_key_name_namespace_id",
                table: "keys",
                columns: new[] { "key_name", "namespace_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_namespaces_users_created_by_user_id",
                table: "namespaces",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
