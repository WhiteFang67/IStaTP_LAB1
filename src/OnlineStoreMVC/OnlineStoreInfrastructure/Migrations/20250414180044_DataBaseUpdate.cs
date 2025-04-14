using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OnlineStoreInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DataBaseUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Departmets",
                table: "DeliveryServices",
                newName: "Departments");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "DeliveryServices",
                columns: new[] { "Id", "Departments", "Name" },
                values: new object[,]
                {
                    { 1, "Усі відділення", "Нова Пошта" },
                    { 2, "Усі відділення", "Укрпошта" }
                });

            migrationBuilder.InsertData(
                table: "StatuseTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "В обробці" },
                    { 2, "Відправлено" },
                    { 3, "Доставлено" },
                    { 4, "Скасовано" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DeliveryServices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DeliveryServices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StatuseTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StatuseTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StatuseTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StatuseTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Departments",
                table: "DeliveryServices",
                newName: "Departmets");
        }
    }
}
