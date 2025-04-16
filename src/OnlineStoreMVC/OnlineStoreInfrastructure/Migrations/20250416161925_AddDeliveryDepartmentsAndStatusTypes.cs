using Microsoft.EntityFrameworkCore.Migrations;

namespace OnlineStoreInfrastructure.Migrations
{
    public partial class AddDeliveryDepartmentsAndStatusTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Змінюємо FK_OrderItems_Orders на Cascade
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders",
                table: "OrderItems");

            // Створюємо StatusTypes
            migrationBuilder.CreateTable(
                name: "StatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusTypes", x => x.Id);
                });

            // Створюємо DeliveryDepartments
            migrationBuilder.CreateTable(
                name: "DeliveryDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryDepartments_DeliveryServices",
                        column: x => x.DeliveryServiceId,
                        principalTable: "DeliveryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Додаємо DeliveryDepartmentId до Orders
            migrationBuilder.AddColumn<int>(
                name: "DeliveryDepartmentId",
                table: "Orders",
                type: "int",
                nullable: false);

            // Створюємо індекси
            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryDepartmentId",
                table: "Orders",
                column: "DeliveryDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDepartments_DeliveryServiceId",
                table: "DeliveryDepartments",
                column: "DeliveryServiceId");

            // Налаштовуємо зовнішні ключі
            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryDepartments",
                table: "Orders",
                column: "DeliveryDepartmentId",
                principalTable: "DeliveryDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_StatusTypes",
                table: "Orders",
                column: "StatusTypeId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryDepartments",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_StatusTypes",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DeliveryDepartments");

            migrationBuilder.DropTable(
                name: "StatusTypes");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryDepartmentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDepartmentId",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}