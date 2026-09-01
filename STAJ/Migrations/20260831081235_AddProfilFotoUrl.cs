using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STAJ.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilFotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilFotoUrl",
                table: "Musteriler",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilFotoUrl",
                table: "Musteriler");
        }
    }
}
