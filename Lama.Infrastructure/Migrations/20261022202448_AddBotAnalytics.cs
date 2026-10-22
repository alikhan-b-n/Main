using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lama.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBotAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotReminderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DelayHours = table.Column<int>(type: "integer", nullable: false),
                    QuietFrom = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    QuietTo = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotReminderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TelegramName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QuestionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Step = table.Column<int>(type: "integer", nullable: true),
                    Detail = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyEvents_OccurredAt",
                table: "SurveyEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyEvents_SessionId",
                table: "SurveyEvents",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotReminderSettings");

            migrationBuilder.DropTable(
                name: "SurveyEvents");
        }
    }
}
