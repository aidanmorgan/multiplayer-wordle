using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionsToSessionRoundAndOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "sessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "rounds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "options",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "version",
                table: "options");
        }
    }
}
