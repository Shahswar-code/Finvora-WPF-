using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finvora.Migrations
{
    /// <inheritdoc />
    public partial class RenameStockPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WholesalePrice",
                table: "StockItems",
                newName: "EndUserPrice");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "StockItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EndUserPrice",
                table: "StockItems",
                newName: "WholesalePrice");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "StockItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }
    }
} 