using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class MealVoucherCreditCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMealVoucher",
                table: "fin_credit_cards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MealVoucherCreditDay",
                table: "fin_credit_cards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MealVoucherDailyAmount",
                table: "fin_credit_cards",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMealVoucher",
                table: "fin_credit_cards");

            migrationBuilder.DropColumn(
                name: "MealVoucherCreditDay",
                table: "fin_credit_cards");

            migrationBuilder.DropColumn(
                name: "MealVoucherDailyAmount",
                table: "fin_credit_cards");
        }
    }
}
