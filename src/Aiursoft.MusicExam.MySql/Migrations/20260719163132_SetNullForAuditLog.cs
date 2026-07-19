using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.MusicExam.MySql.Migrations
{
    /// <inheritdoc />
    public partial class SetNullForAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Changes_AspNetUsers_TriggerUserId",
                table: "Changes");

            migrationBuilder.AddForeignKey(
                name: "FK_Changes_AspNetUsers_TriggerUserId",
                table: "Changes",
                column: "TriggerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Changes_AspNetUsers_TriggerUserId",
                table: "Changes");

            migrationBuilder.AddForeignKey(
                name: "FK_Changes_AspNetUsers_TriggerUserId",
                table: "Changes",
                column: "TriggerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
