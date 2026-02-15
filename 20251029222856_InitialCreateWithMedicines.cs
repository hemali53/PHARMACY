using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHARMACY.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithMedicines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
        name: "Medicines",
        columns: table => new
        {
            MedicineID = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
            Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
            ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
            CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Medicines", x => x.MedicineID);
        });

            migrationBuilder.CreateTable(
                name: "MedicineBatches",
                columns: table => new
                {
                    BatchID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicineID = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineBatches", x => x.BatchID);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Medicines_MedicineID",
                        column: x => x.MedicineID,
                        principalTable: "Medicines",
                        principalColumn: "MedicineID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedicineID",
                table: "MedicineBatches",
                column: "MedicineID");
        }
    }
}