using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.MigrationsWarehouse
{
    /// <inheritdoc />
    public partial class InitialWarehouseCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsClimateControlled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    StandardCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastRestockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("CK_WarehouseInventoryItems_ReorderLevel", "[ReorderLevel] >= 0");
                    table.CheckConstraint("CK_WarehouseInventoryItems_StandardCost", "[StandardCost] >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseLocationId = table.Column<int>(type: "int", nullable: false),
                    BinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    LastCountedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBins", x => x.Id);
                    table.CheckConstraint("CK_WarehouseStockBins_QuantityOnHand", "[QuantityOnHand] >= 0");
                    table.ForeignKey(
                        name: "FK_StockBins_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockBins_WarehouseLocations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "ContactEmail", "IsPreferred", "Name" },
                values: new object[,]
                {
                    { 1, "ops@northwind.example", true, "Northwind Storage" },
                    { 2, "inventory@blueyonder.example", false, "Blue Yonder Parts" }
                });

            migrationBuilder.InsertData(
                table: "WarehouseLocations",
                columns: new[] { "Id", "IsClimateControlled", "Name", "RegionCode" },
                values: new object[,]
                {
                    { 1, true, "Seattle Fulfillment", "US-WEST" },
                    { 2, false, "Austin Overflow", "US-CENTRAL" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "LastRestockedAt", "Name", "ReorderLevel", "Sku", "StandardCost", "SupplierId" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 2, 4, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Stackable storage bin", 40, "BIN-STACK-001", 14.50m, 1 },
                    { 2, new DateTimeOffset(new DateTime(2026, 2, 7, 9, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Handheld barcode scanner", 8, "SCAN-HAND-002", 89.00m, 1 },
                    { 3, new DateTimeOffset(new DateTime(2026, 2, 2, 11, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Thermal label refill pack", 25, "LABEL-THERM-003", 32.75m, 2 }
                });

            migrationBuilder.InsertData(
                table: "StockBins",
                columns: new[] { "Id", "BinCode", "InventoryItemId", "LastCountedAt", "QuantityOnHand", "WarehouseLocationId" },
                values: new object[,]
                {
                    { 1, "SEA-A01", 1, new DateTimeOffset(new DateTime(2026, 2, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 128, 1 },
                    { 2, "SEA-B07", 2, new DateTimeOffset(new DateTime(2026, 2, 8, 8, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 18, 1 },
                    { 3, "AUS-C03", 3, new DateTimeOffset(new DateTime(2026, 2, 8, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 64, 2 },
                    { 4, "AUS-A02", 1, new DateTimeOffset(new DateTime(2026, 2, 8, 9, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 42, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SupplierId_Name",
                table: "InventoryItems",
                columns: new[] { "SupplierId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBins_InventoryItemId_WarehouseLocationId",
                table: "StockBins",
                columns: new[] { "InventoryItemId", "WarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBins_WarehouseLocationId_BinCode",
                table: "StockBins",
                columns: new[] { "WarehouseLocationId", "BinCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ContactEmail",
                table: "Suppliers",
                column: "ContactEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_Name",
                table: "WarehouseLocations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockBins");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
