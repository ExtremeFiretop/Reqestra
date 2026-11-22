using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ombi.Store.Context.Postgres;

#nullable disable

namespace Ombi.Store.Migrations.OmbiPostgres
{
    [DbContext(typeof(OmbiPostgresContext))]
    [Migration("20261122000100_AddUserEmailNotificationPreferences")]
    public partial class AddUserEmailNotificationPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmailNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmailNotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailNotificationPreferences_UserId_NotificationType",
                table: "UserEmailNotificationPreferences",
                columns: new[] { "UserId", "NotificationType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserEmailNotificationPreferences");
        }
    }
}
