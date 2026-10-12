using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlphaB.Migrations
{
    public partial class AddSinifSeviyesiToDersler : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PLESK UYUMLU, RAW SQL YOK — KESİN ÇÖZÜM
            migrationBuilder.AddColumn<int>(
                name: "SinifSeviyesi",
                table: "Dersler",
                type: "int",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SinifSeviyesi",
                table: "Dersler"
            );
        }
    }
}
