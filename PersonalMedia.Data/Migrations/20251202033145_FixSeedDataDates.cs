using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalMedia.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedDataDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "GenerationSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastGenerationDate", "ModifiedDate" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "GenerationSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastGenerationDate", "ModifiedDate" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 12, 2, 3, 11, 49, 153, DateTimeKind.Utc).AddTicks(7770) });
        }
    }
}
