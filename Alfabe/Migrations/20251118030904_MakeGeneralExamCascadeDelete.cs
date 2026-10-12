using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    /// <inheritdoc />
    public partial class MakeGeneralExamCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneralExams_Dersler_DersId",
                table: "GeneralExams");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralExams_Dersler_DersId",
                table: "GeneralExams",
                column: "DersId",
                principalTable: "Dersler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneralExams_Dersler_DersId",
                table: "GeneralExams");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralExams_Dersler_DersId",
                table: "GeneralExams",
                column: "DersId",
                principalTable: "Dersler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
