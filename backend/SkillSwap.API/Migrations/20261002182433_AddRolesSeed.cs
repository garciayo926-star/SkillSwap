using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillSwap.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL no convierte text -> timestamp automáticamente, se requiere USING
            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateProposed\" TYPE timestamp with time zone " +
                "USING \"DateProposed\"::timestamp with time zone;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateCompleted\" DROP NOT NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateCompleted\" TYPE timestamp with time zone " +
                "USING NULLIF(\"DateCompleted\", '')::timestamp with time zone;");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Master" },
                    { 2, "Technical" },
                    { 3, "Student" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateProposed\" TYPE text " +
                "USING \"DateProposed\"::text;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateCompleted\" TYPE text " +
                "USING COALESCE(\"DateCompleted\"::text, '');");

            migrationBuilder.Sql(
                "ALTER TABLE \"Exchanges\" ALTER COLUMN \"DateCompleted\" SET NOT NULL;");
        }
    }
}
