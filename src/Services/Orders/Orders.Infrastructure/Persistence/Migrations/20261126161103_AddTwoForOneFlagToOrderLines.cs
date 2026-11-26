using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comandas.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoForOneFlagToOrderLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoForOneEligible",
                table: "OrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwoForOneEligible",
                table: "OrderLines");
        }
    }
}
