using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedOtpCount",
                table: "UserAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpLockedUntilUtc",
                table: "UserAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodeHashes",
                table: "UserAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "TelegramConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EncryptedBotToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    BotUsername = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramConnections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramConnections");

            migrationBuilder.DropColumn(
                name: "FailedOtpCount",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "OtpLockedUntilUtc",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "RecoveryCodeHashes",
                table: "UserAccounts");
        }
    }
}
