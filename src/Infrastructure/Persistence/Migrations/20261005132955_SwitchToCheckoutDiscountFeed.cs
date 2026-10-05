using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToCheckoutDiscountFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE DiscountPrograms SET Status = 4 WHERE Status NOT IN (1, 9);");

            migrationBuilder.DropTable(
                name: "ProgramProductPrices");

            migrationBuilder.DropColumn(
                name: "IsStopRequested",
                table: "DiscountPrograms");

            migrationBuilder.DropColumn(
                name: "Rounding",
                table: "DiscountPrograms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStopRequested",
                table: "DiscountPrograms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Rounding",
                table: "DiscountPrograms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProgramProductPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppliedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DiscountedPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OriginalPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    RestoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramProductPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramProductPrices_DiscountPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "DiscountPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramProductPrices_ProductId",
                table: "ProgramProductPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramProductPrices_ProgramId_ProductId",
                table: "ProgramProductPrices",
                columns: new[] { "ProgramId", "ProductId" },
                unique: true);
        }
    }
}
