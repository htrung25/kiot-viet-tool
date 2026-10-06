using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicesReconciledToUtc",
                table: "KiotVietConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountFeedSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProgramsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountFeedSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceReconciliations",
                columns: table => new
                {
                    InvoiceId = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PurchasedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SoldByName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Total = table.Column<string>(type: "TEXT", nullable: false),
                    ActualDiscount = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedDiscount = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramNames = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LinesJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReconciledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReconciliations", x => x.InvoiceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountFeedSnapshots_PublishedAtUtc",
                table: "DiscountFeedSnapshots",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReconciliations_IsReviewed_Outcome",
                table: "InvoiceReconciliations",
                columns: new[] { "IsReviewed", "Outcome" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReconciliations_PurchasedAtUtc",
                table: "InvoiceReconciliations",
                column: "PurchasedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountFeedSnapshots");

            migrationBuilder.DropTable(
                name: "InvoiceReconciliations");

            migrationBuilder.DropColumn(
                name: "InvoicesReconciledToUtc",
                table: "KiotVietConnections");
        }
    }
}
