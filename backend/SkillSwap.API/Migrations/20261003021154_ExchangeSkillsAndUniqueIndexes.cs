using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillSwap.API.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeSkillsAndUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_StudentId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Offers_StudentId",
                table: "Offers");

            migrationBuilder.AddColumn<int>(
                name: "OfferedSkillId",
                table: "Exchanges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedSkillId",
                table: "Exchanges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_StudentId_SkillId",
                table: "Requests",
                columns: new[] { "StudentId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_StudentId_SkillId",
                table: "Offers",
                columns: new[] { "StudentId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_OfferedSkillId",
                table: "Exchanges",
                column: "OfferedSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_RequestedSkillId",
                table: "Exchanges",
                column: "RequestedSkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exchanges_Skills_OfferedSkillId",
                table: "Exchanges",
                column: "OfferedSkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Exchanges_Skills_RequestedSkillId",
                table: "Exchanges",
                column: "RequestedSkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exchanges_Skills_OfferedSkillId",
                table: "Exchanges");

            migrationBuilder.DropForeignKey(
                name: "FK_Exchanges_Skills_RequestedSkillId",
                table: "Exchanges");

            migrationBuilder.DropIndex(
                name: "IX_Requests_StudentId_SkillId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Offers_StudentId_SkillId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Exchanges_OfferedSkillId",
                table: "Exchanges");

            migrationBuilder.DropIndex(
                name: "IX_Exchanges_RequestedSkillId",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "OfferedSkillId",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "RequestedSkillId",
                table: "Exchanges");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_StudentId",
                table: "Requests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_StudentId",
                table: "Offers",
                column: "StudentId");
        }
    }
}
