using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHARMACY.Migrations
{
    /// <inheritdoc />
    public partial class AddVATFieldsToMedicine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MinimumStockLevel",
                table: "Medicines",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                table: "Medicines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWithVAT",
                table: "Medicines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VATAmount",
                table: "Medicines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VATPercentage",
                table: "Medicines",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageCost",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "TotalWithVAT",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "VATAmount",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "VATPercentage",
                table: "Medicines");

            migrationBuilder.AlterColumn<int>(
                name: "MinimumStockLevel",
                table: "Medicines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
