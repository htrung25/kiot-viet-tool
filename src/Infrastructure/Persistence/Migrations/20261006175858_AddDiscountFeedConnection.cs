using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    public partial class AddDiscountFeedConnection : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscountFeedConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    EncryptedWriteToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EncryptedReadToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CloudflareAccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ScriptName = table.Column<string>(type: "TEXT", maxLength: 63, nullable: true),
                    InstanceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastRevision = table.Column<long>(type: "INTEGER", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountFeedConnections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountFeedConnections");
        }
    }
}
