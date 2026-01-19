using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.MusicExam.Sqlite.Migrations
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
                type: "TEXT",
                nullable: true);
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
