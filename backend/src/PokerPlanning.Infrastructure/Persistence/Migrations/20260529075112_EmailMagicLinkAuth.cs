using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailMagicLinkAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_login_tokens",
                schema: "poker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    return_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_login_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "poker",
                columns: table => new
                {
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subject = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.user_id, x.provider, x.subject });
                    table.ForeignKey(
                        name: "FK_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "poker",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_login_tokens_token_hash",
                schema: "poker",
                table: "email_login_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_provider_subject",
                schema: "poker",
                table: "user_logins",
                columns: new[] { "provider", "subject" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO poker.user_logins (user_id, provider, subject)
                SELECT u.id, login->>'provider', login->>'subject'
                FROM poker.users AS u
                CROSS JOIN LATERAL jsonb_array_elements(u.logins::jsonb) AS login
                WHERE u.logins IS NOT NULL
                  AND u.logins <> ''
                  AND login ? 'provider'
                  AND login ? 'subject';
                """);

            migrationBuilder.DropColumn(
                name: "logins",
                schema: "poker",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logins",
                schema: "poker",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE poker.users AS u
                SET logins = COALESCE(login_data.logins, '[]')
                FROM (
                    SELECT user_id, jsonb_agg(
                        jsonb_build_object('provider', provider, 'subject', subject)
                        ORDER BY provider, subject
                    )::text AS logins
                    FROM poker.user_logins
                    GROUP BY user_id
                ) AS login_data
                WHERE u.id = login_data.user_id;
                """);

            migrationBuilder.DropTable(
                name: "email_login_tokens",
                schema: "poker");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "poker");
        }
    }
}
