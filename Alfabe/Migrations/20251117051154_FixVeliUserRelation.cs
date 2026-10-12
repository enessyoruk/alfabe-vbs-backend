using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class FixVeliUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli");

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli",
                column: "AdresId",
                principalTable: "Adres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli");

            migrationBuilder.AddForeignKey(
                name: "FK_Veli_Adres_AdresId",
                table: "Veli",
                column: "AdresId",
                principalTable: "Adres",
                principalColumn: "Id");
        }
    }
}
