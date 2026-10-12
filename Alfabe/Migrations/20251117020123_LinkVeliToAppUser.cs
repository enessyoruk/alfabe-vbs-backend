using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class LinkVeliToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AdresId",
                table: "Veli",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Veli",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veli_AppUserId",
                table: "Veli",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli");

            migrationBuilder.DropIndex(
                name: "IX_Veli_AppUserId",
                table: "Veli");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Veli");

            migrationBuilder.AlterColumn<int>(
                name: "AdresId",
                table: "Veli",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
