using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class GitLikeVersioningSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_keys_versions_current_version_id",
                table: "keys");

            migrationBuilder.DropTable(
                name: "versions");

            migrationBuilder.DropTable(
                name: "keys");

            migrationBuilder.AddColumn<int>(
                name: "snapshot_interval",
                table: "namespaces",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateTable(
                name: "commits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hash = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    author_user_id = table.Column<int>(type: "integer", nullable: false),
                    namespace_id = table.Column<int>(type: "integer", nullable: false),
                    generation = table.Column<int>(type: "integer", nullable: false),
                    parent_commit_id = table.Column<int>(type: "integer", nullable: true),
                    merge_parent_commit_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commits", x => x.id);
                    table.ForeignKey(
                        name: "fk_commits_commits_merge_parent_commit_id",
                        column: x => x.merge_parent_commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_commits_commits_parent_commit_id",
                        column: x => x.parent_commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_commits_namespaces_namespace_id",
                        column: x => x.namespace_id,
                        principalTable: "namespaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commits_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    namespace_id = table.Column<int>(type: "integer", nullable: false),
                    head_commit_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_commits_head_commit_id",
                        column: x => x.head_commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_branches_namespaces_namespace_id",
                        column: x => x.namespace_id,
                        principalTable: "namespaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commit_changes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    commit_id = table.Column<int>(type: "integer", nullable: false),
                    key_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commit_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_commit_changes_commits_commit_id",
                        column: x => x.commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commit_snapshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    commit_id = table.Column<int>(type: "integer", nullable: false),
                    state_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commit_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_commit_snapshots_commits_commit_id",
                        column: x => x.commit_id,
                        principalTable: "commits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    key_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    last_modified_commit_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_states_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unstaged_changes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    key_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unstaged_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_unstaged_changes_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_states_branch_id",
                table: "branch_states",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_branch_states_branch_id_key_name",
                table: "branch_states",
                columns: new[] { "branch_id", "key_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_head_commit_id",
                table: "branches",
                column: "head_commit_id");

            migrationBuilder.CreateIndex(
                name: "ix_branches_name_namespace_id",
                table: "branches",
                columns: new[] { "name", "namespace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_namespace_id",
                table: "branches",
                column: "namespace_id");

            migrationBuilder.CreateIndex(
                name: "ix_commit_changes_commit_id",
                table: "commit_changes",
                column: "commit_id");

            migrationBuilder.CreateIndex(
                name: "ix_commit_changes_commit_id_key_name",
                table: "commit_changes",
                columns: new[] { "commit_id", "key_name" });

            migrationBuilder.CreateIndex(
                name: "ix_commit_snapshots_commit_id",
                table: "commit_snapshots",
                column: "commit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commits_author_user_id",
                table: "commits",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_commits_generation",
                table: "commits",
                columns: new[] { "namespace_id", "generation" });

            migrationBuilder.CreateIndex(
                name: "ix_commits_hash_namespace_id",
                table: "commits",
                columns: new[] { "hash", "namespace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commits_merge_parent_commit_id",
                table: "commits",
                column: "merge_parent_commit_id",
                filter: "merge_parent_commit_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_commits_namespace_id",
                table: "commits",
                column: "namespace_id");

            migrationBuilder.CreateIndex(
                name: "ix_commits_parent_commit_id",
                table: "commits",
                column: "parent_commit_id");

            migrationBuilder.CreateIndex(
                name: "ix_unstaged_changes_branch_id",
                table: "unstaged_changes",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_unstaged_changes_branch_id_key_name",
                table: "unstaged_changes",
                columns: new[] { "branch_id", "key_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_states");

            migrationBuilder.DropTable(
                name: "commit_changes");

            migrationBuilder.DropTable(
                name: "commit_snapshots");

            migrationBuilder.DropTable(
                name: "unstaged_changes");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "commits");

            migrationBuilder.DropColumn(
                name: "snapshot_interval",
                table: "namespaces");

            migrationBuilder.CreateTable(
                name: "keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    current_version_id = table.Column<int>(type: "integer", nullable: true),
                    namespace_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    key_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_keys_namespaces_namespace_id",
                        column: x => x.namespace_id,
                        principalTable: "namespaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    value = table.Column<string>(type: "text", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false)
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
                name: "ix_keys_current_version_id",
                table: "keys",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_keys_is_deleted_true",
                table: "keys",
                column: "is_deleted",
                filter: "is_deleted = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_keys_namespace_id",
                table: "keys",
                column: "namespace_id");

            migrationBuilder.CreateIndex(
                name: "ix_keys_unique_active_key_name_namespace_id",
                table: "keys",
                columns: new[] { "key_name", "namespace_id" },
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_versions_key_id",
                table: "versions",
                column: "key_id");

            migrationBuilder.CreateIndex(
                name: "ix_versions_key_id_version_number",
                table: "versions",
                columns: new[] { "key_id", "version_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_keys_versions_current_version_id",
                table: "keys",
                column: "current_version_id",
                principalTable: "versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
