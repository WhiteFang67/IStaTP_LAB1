using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStoreInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "OrderItems",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrderItems");
        }
    }
}
