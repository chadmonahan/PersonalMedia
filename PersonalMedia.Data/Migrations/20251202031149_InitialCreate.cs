using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PersonalMedia.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BasePersonImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AzureStorageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasePersonImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenerationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailySetsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ImagesPerSetMin = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    ImagesPerSetMax = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    MaxRetryAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    ModestyLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "Family Friendly"),
                    LastGenerationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParameterOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Weight = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MediaSetId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    AzureStorageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BasePersonImageId = table.Column<int>(type: "int", nullable: true),
                    GenerationPrompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GenerationStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    GenerationStartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GenerationCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItems_BasePersonImages_BasePersonImageId",
                        column: x => x.BasePersonImageId,
                        principalTable: "BasePersonImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MediaItems_MediaSets_MediaSetId",
                        column: x => x.MediaSetId,
                        principalTable: "MediaSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MediaItemId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationParameters_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MediaItemId = table.Column<int>(type: "int", nullable: false),
                    ReactionType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaReactions_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "GenerationSettings",
                columns: new[] { "Id", "DailySetsCount", "ImagesPerSetMax", "ImagesPerSetMin", "LastGenerationDate", "MaxRetryAttempts", "ModestyLevel", "ModifiedDate" },
                values: new object[] { 1, 5, 5, 3, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Family Friendly", new DateTime(2025, 12, 2, 3, 11, 49, 153, DateTimeKind.Utc).AddTicks(7770) });

            migrationBuilder.InsertData(
                table: "ParameterOptions",
                columns: new[] { "Id", "Category", "IsActive", "Value", "Weight" },
                values: new object[,]
                {
                    { 1, 1, true, "Beach", 1 },
                    { 2, 1, true, "Mountain", 1 },
                    { 3, 1, true, "City", 1 },
                    { 4, 1, true, "Forest", 1 },
                    { 5, 1, true, "Park", 1 },
                    { 6, 2, true, "Happy", 1 },
                    { 7, 2, true, "Peaceful", 1 },
                    { 8, 2, true, "Excited", 1 },
                    { 9, 2, true, "Contemplative", 1 },
                    { 10, 3, true, "Reading", 1 },
                    { 11, 3, true, "Walking", 1 },
                    { 12, 3, true, "Picnic", 1 },
                    { 13, 3, true, "Photography", 1 },
                    { 14, 3, true, "Relaxing", 1 },
                    { 15, 4, true, "Casual", 1 },
                    { 16, 4, true, "Athletic", 1 },
                    { 17, 4, true, "Smart Casual", 1 },
                    { 18, 5, true, "Morning", 1 },
                    { 19, 5, true, "Afternoon", 1 },
                    { 20, 5, true, "Evening", 1 },
                    { 21, 5, true, "Golden Hour", 1 },
                    { 22, 6, true, "Sunny", 1 },
                    { 23, 6, true, "Cloudy", 1 },
                    { 24, 6, true, "Partly Cloudy", 1 },
                    { 25, 7, true, "Photorealistic", 1 },
                    { 26, 7, true, "Cinematic", 1 },
                    { 27, 7, true, "Portrait", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationParameters_MediaItemId_Category",
                table: "GenerationParameters",
                columns: new[] { "MediaItemId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_BasePersonImageId",
                table: "MediaItems",
                column: "BasePersonImageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_MediaSetId",
                table: "MediaItems",
                column: "MediaSetId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaReactions_MediaItemId_ReactionType",
                table: "MediaReactions",
                columns: new[] { "MediaItemId", "ReactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterOptions_Category_IsActive",
                table: "ParameterOptions",
                columns: new[] { "Category", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationParameters");

            migrationBuilder.DropTable(
                name: "GenerationSettings");

            migrationBuilder.DropTable(
                name: "MediaReactions");

            migrationBuilder.DropTable(
                name: "ParameterOptions");

            migrationBuilder.DropTable(
                name: "MediaItems");

            migrationBuilder.DropTable(
                name: "BasePersonImages");

            migrationBuilder.DropTable(
                name: "MediaSets");
        }
    }
}
