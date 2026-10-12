using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserIdToOgretmen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Ogretmen",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Ogretmen");
        }
    }
}
