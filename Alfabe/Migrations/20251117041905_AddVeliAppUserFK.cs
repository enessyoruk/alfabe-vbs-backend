using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class AddVeliAppUserFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli");

            migrationBuilder.DropForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Veli");

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli",
                column: "AdresId",
                principalTable: "Adres",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli");

            migrationBuilder.DropForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Veli",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli",
                column: "AdresId",
                principalTable: "Adres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_AspNetUsers_AppUserId",
                table: "Veli",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
