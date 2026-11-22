using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ombi.Store.Context.MySql;

#nullable disable

namespace Ombi.Store.Migrations.OmbiMySql
{
    [DbContext(typeof(OmbiMySqlContext))]
    [Migration("20261122000100_AddUserEmailNotificationPreferences")]
    public partial class AddUserEmailNotificationPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmailNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", Microsoft.EntityFrameworkCore.Metadata.MySqlValueGenerationStrategy.IdentityColumn),
                    // Older MySQL installs can use a different charset/collation for
                    // AspNetUsers.Id. Do not force utf8mb4 here or create a foreign key
                    // across incompatible legacy user ID definitions.
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    // MySQL intentionally omits the FK. The existing user key's
                    // charset/collation varies across older installations. User
                    // deletion explicitly removes these preference rows instead.
                    table.PrimaryKey("PK_UserEmailNotificationPreferences", x => x.Id);
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
