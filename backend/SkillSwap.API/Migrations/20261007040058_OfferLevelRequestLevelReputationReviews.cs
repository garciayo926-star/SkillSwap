using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillSwap.API.Migrations
{
    /// <inheritdoc />
    public partial class OfferLevelRequestLevelReputationReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Reputation",
                table: "Students",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedByStudentId",
                table: "Skills",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesiredLevel",
                table: "Requests",
                type: "text",
                nullable: false,
                defaultValue: "Básico");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Offers",
                type: "text",
                nullable: false,
                defaultValue: "Básico");

            migrationBuilder.AddColumn<string>(
                name: "Modality",
                table: "Offers",
                type: "text",
                nullable: false,
                defaultValue: "Ambas");

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionDate",
                table: "Exchanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExchangeId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerStudentId = table.Column<int>(type: "integer", nullable: false),
                    RevieweeStudentId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Students_RevieweeStudentId",
                        column: x => x.RevieweeStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Students_ReviewerStudentId",
                        column: x => x.ReviewerStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_SuggestedByStudentId",
                table: "Skills",
                column: "SuggestedByStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ExchangeId",
                table: "Reviews",
                column: "ExchangeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RevieweeStudentId",
                table: "Reviews",
                column: "RevieweeStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerStudentId",
                table: "Reviews",
                column: "ReviewerStudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Students_SuggestedByStudentId",
                table: "Skills",
                column: "SuggestedByStudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Students_SuggestedByStudentId",
                table: "Skills");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Skills_SuggestedByStudentId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Reputation",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SuggestedByStudentId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "DesiredLevel",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "SessionDate",
                table: "Exchanges");
        }
    }
}
