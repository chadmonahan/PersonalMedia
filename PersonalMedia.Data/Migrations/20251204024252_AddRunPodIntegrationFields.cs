using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalMedia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRunPodIntegrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExecutionTimeMs",
                table: "MediaItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JobSubmittedDate",
                table: "MediaItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawWebhookPayload",
                table: "MediaItems",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RunPodJobId",
                table: "MediaItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WebhookReceivedDate",
                table: "MediaItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RunPodWebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WasProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunPodWebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_RunPodJobId",
                table: "MediaItems",
                column: "RunPodJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPodWebhookLogs_JobId",
                table: "RunPodWebhookLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPodWebhookLogs_ReceivedDate",
                table: "RunPodWebhookLogs",
                column: "ReceivedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunPodWebhookLogs");

            migrationBuilder.DropIndex(
                name: "IX_MediaItems_RunPodJobId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "ExecutionTimeMs",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "JobSubmittedDate",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "RawWebhookPayload",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "RunPodJobId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "WebhookReceivedDate",
                table: "MediaItems");
        }
    }
}
