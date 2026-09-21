using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lama.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadPriorityAndNextStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextStep",
                table: "Leads",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityPriority",
                table: "Leads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextStep",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UniversityPriority",
                table: "Leads");
        }
    }
}
