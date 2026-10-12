using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class MakeOgrenciDersCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OgrenciDersler_Dersler_DersId",
                table: "OgrenciDersler");

            migrationBuilder.AddForeignKey(
                name: "FK_OgrenciDersler_Dersler_DersId",
                table: "OgrenciDersler",
                column: "DersId",
                principalTable: "Dersler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OgrenciDersler_Dersler_DersId",
                table: "OgrenciDersler");

            migrationBuilder.AddForeignKey(
                name: "FK_OgrenciDersler_Dersler_DersId",
                table: "OgrenciDersler",
                column: "DersId",
                principalTable: "Dersler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
