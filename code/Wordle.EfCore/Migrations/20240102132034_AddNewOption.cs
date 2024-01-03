using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddNewOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "roundendtoleranceseconds",
                table: "options",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "roundendtoleranceseconds",
                table: "options");
        }
    }
}
