using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStoreInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDepartmentsFromDeliveryService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Departments",
                table: "DeliveryServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Departments",
                table: "DeliveryServices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
