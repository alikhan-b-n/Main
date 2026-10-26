using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lama.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stockists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Stage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastContactedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stockists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockistEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    PreviousQuantity = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FromStage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ToStage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockistEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockistEvents_Stockists_StockistId",
                        column: x => x.StockistId,
                        principalTable: "Stockists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockistInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockistId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockistInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockistInvoices_Stockists_StockistId",
                        column: x => x.StockistId,
                        principalTable: "Stockists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockistStock",
                columns: table => new
                {
                    Title = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StockistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockistStock", x => new { x.StockistId, x.Title });
                    table.ForeignKey(
                        name: "FK_StockistStock_Stockists_StockistId",
                        column: x => x.StockistId,
                        principalTable: "Stockists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockistEvents_StockistId_OccurredOn",
                table: "StockistEvents",
                columns: new[] { "StockistId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_StockistInvoices_StockistId_SentOn",
                table: "StockistInvoices",
                columns: new[] { "StockistId", "SentOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Stockists_Name",
                table: "Stockists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Stockists_Stage",
                table: "Stockists",
                column: "Stage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockistEvents");

            migrationBuilder.DropTable(
                name: "StockistInvoices");

            migrationBuilder.DropTable(
                name: "StockistStock");

            migrationBuilder.DropTable(
                name: "Stockists");
        }
    }
}
