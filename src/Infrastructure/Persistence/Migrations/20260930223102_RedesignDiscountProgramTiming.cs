using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignDiscountProgramTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscountPrograms_TargetPriceBookId",
                table: "DiscountPrograms");

            migrationBuilder.DropColumn(
                name: "TargetPriceBookId",
                table: "DiscountPrograms");

            migrationBuilder.AddColumn<int>(
                name: "StartMode",
                table: "DiscountPrograms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartAtUtc",
                table: "DiscountPrograms",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountPrograms_Status",
                table: "DiscountPrograms",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscountPrograms_Status",
                table: "DiscountPrograms");

            migrationBuilder.DropColumn(
                name: "StartMode",
                table: "DiscountPrograms");

            migrationBuilder.Sql("DELETE FROM DiscountPrograms;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartAtUtc",
                table: "DiscountPrograms",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetPriceBookId",
                table: "DiscountPrograms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountPrograms_TargetPriceBookId",
                table: "DiscountPrograms",
                column: "TargetPriceBookId",
                unique: true);
        }
    }
}
