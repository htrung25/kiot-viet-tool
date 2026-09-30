using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiotVietTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKiotVietCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KiotVietConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Retailer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptedClientSecret = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProductsSyncedFromUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiotVietConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceBookItems",
                columns: table => new
                {
                    PriceBookId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceBookItems", x => new { x.PriceBookId, x.ProductId });
                });

            migrationBuilder.CreateTable(
                name: "PriceBooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ForAllCustomerGroups = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForAllUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    BranchIds = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerGroupIds = table.Column<string>(type: "TEXT", nullable: false),
                    UserIds = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    BasePrice = table.Column<long>(type: "INTEGER", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MasterUnitId = table.Column<long>(type: "INTEGER", nullable: true),
                    ConversionValue = table.Column<double>(type: "REAL", nullable: true),
                    MasterProductId = table.Column<long>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowsSale = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SearchKey = table.Column<string>(type: "TEXT", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceBookItems_ProductId",
                table: "PriceBookItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MasterUnitId",
                table: "Products",
                column: "MasterUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "KiotVietConnections");

            migrationBuilder.DropTable(
                name: "PriceBookItems");

            migrationBuilder.DropTable(
                name: "PriceBooks");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
