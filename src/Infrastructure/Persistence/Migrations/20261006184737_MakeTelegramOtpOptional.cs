using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    public partial class MakeTelegramOtpOptional : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoginOtpEnabled",
                table: "TelegramConnections",
                type: "INTEGER",
                nullable: false,
                // Existing connections were made when OTP was mandatory: keep it on after the upgrade.
                defaultValue: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoginOtpEnabled",
                table: "TelegramConnections");
        }
    }
}
