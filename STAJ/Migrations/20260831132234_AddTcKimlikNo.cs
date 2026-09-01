using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STAJ.Migrations
{
    /// <inheritdoc />
    public partial class AddTcKimlikNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "Musteriler",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_TcKimlikNo",
                table: "Musteriler",
                column: "TcKimlikNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Musteriler_TcKimlikNo",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "Musteriler");
        }
    }
}
