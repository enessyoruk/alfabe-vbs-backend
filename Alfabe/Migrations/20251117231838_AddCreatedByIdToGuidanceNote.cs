using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByIdToGuidanceNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ogrenci_Veli_VeliId",
                table: "Ogrenci");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "GuidanceNotes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Ogrenci_Veli_VeliId",
                table: "Ogrenci",
                column: "VeliId",
                principalTable: "Veli",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ogrenci_Veli_VeliId",
                table: "Ogrenci");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "GuidanceNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_Ogrenci_Veli_VeliId",
                table: "Ogrenci",
                column: "VeliId",
                principalTable: "Veli",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
