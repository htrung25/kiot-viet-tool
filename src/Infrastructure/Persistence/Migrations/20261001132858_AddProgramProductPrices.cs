using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramProductPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAtUtc",
                table: "DiscountPrograms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStopRequested",
                table: "DiscountPrograms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProgramProductPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OriginalPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    DiscountedPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RestoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramProductPrices");

            migrationBuilder.DropColumn(
                name: "FinishedAtUtc",
                table: "DiscountPrograms");

            migrationBuilder.DropColumn(
                name: "IsStopRequested",
                table: "DiscountPrograms");
        }
    }
}
