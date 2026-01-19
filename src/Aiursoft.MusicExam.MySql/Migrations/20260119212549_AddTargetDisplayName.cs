using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.MusicExam.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetDisplayName",
                table: "Changes",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetDisplayName",
                table: "Changes");
        }
    }
}
