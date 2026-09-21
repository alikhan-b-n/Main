using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lama.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TelegramUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TelegramId = table.Column<long>(type: "bigint", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ApplicantType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AgeRange = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CurrentEducation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TargetDegree = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IntakeYear = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TargetCountries = table.Column<List<string>>(type: "text[]", nullable: false),
                    FundingNeed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AnnualBudget = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Gpa = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EnglishLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EnglishCertificate = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EnglishScore = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FieldsOfInterest = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ServicesNeeded = table.Column<List<string>>(type: "text[]", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LostReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SurveySummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadEvents_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadEvents_LeadId_CreatedAt",
                table: "LeadEvents",
                columns: new[] { "LeadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CreatedAt",
                table: "Leads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Email",
                table: "Leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ExternalId",
                table: "Leads",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Source",
                table: "Leads",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TelegramId",
                table: "Leads",
                column: "TelegramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadEvents");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
