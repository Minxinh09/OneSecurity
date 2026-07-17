using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "IncidentId",
                table: "alerts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incidents_AspNetUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_incidents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_IncidentId",
                table: "alerts",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_AssignedUserId",
                table: "incidents",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_CreatedAt",
                table: "incidents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_CreatedByUserId",
                table: "incidents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Severity",
                table: "incidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Status",
                table: "incidents",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_incidents_IncidentId",
                table: "alerts",
                column: "IncidentId",
                principalTable: "incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alerts_incidents_IncidentId",
                table: "alerts");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropIndex(
                name: "IX_alerts_IncidentId",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "alerts");
        }
    }
}
