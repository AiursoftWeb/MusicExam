using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.MusicExam.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Changes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerUserId = table.Column<string>(type: "TEXT", nullable: true),
                    TargetUserId = table.Column<string>(type: "TEXT", nullable: true),
                    TargetRoleId = table.Column<string>(type: "TEXT", nullable: true),
                    TargetPermission = table.Column<string>(type: "TEXT", nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Changes_AspNetUsers_TriggerUserId",
                        column: x => x.TriggerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Changes_TriggerUserId",
                table: "Changes",
                column: "TriggerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Changes");
        }
    }
}
