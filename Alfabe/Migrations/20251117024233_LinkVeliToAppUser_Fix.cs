using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class LinkVeliToAppUser_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Veli",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Veli");
        }
    }
}
