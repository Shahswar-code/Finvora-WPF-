using Finvora.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finvora.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FinvoraDbContext))]
    [Migration("20260829010000_RenameStockPricingFields")]
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