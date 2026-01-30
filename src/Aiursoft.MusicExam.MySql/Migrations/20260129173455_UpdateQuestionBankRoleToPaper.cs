using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.MusicExam.MySql.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionBankRoleToPaper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionBankRoles_Schools_SchoolId",
                table: "QuestionBankRoles");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "QuestionBankRoles",
                newName: "ExamPaperId");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionBankRoles_SchoolId",
                table: "QuestionBankRoles",
                newName: "IX_QuestionBankRoles_ExamPaperId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionBankRoles_ExamPapers_ExamPaperId",
                table: "QuestionBankRoles",
                column: "ExamPaperId",
                principalTable: "ExamPapers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionBankRoles_ExamPapers_ExamPaperId",
                table: "QuestionBankRoles");

            migrationBuilder.RenameColumn(
                name: "ExamPaperId",
                table: "QuestionBankRoles",
                newName: "SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionBankRoles_ExamPaperId",
                table: "QuestionBankRoles",
                newName: "IX_QuestionBankRoles_SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionBankRoles_Schools_SchoolId",
                table: "QuestionBankRoles",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
