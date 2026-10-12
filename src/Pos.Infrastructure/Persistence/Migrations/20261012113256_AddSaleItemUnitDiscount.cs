using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleItemUnitDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitDiscount",
                table: "SaleItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Existing line discounts were entered per line; express them per unit.
            migrationBuilder.Sql("UPDATE [SaleItems] SET [UnitDiscount] = ROUND([ManualDiscount] / [Quantity], 4) WHERE [ManualDiscount] > 0 AND [Quantity] > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitDiscount",
                table: "SaleItems");
        }
    }
}
