using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "alert_rules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "alert_rules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "alert_rules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AgentVersion",
                table: "agents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Capabilities",
                table: "agents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectorVersion",
                table: "agents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportedActions",
                table: "agents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "response_actions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IncidentId = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResultMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_response_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_response_actions_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_response_actions_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_response_actions_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_response_actions_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-db-01",
                columns: new[] { "AgentVersion", "Capabilities", "CollectorVersion", "SupportedActions" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-web-02",
                columns: new[] { "AgentVersion", "Capabilities", "CollectorVersion", "SupportedActions" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-app-01",
                columns: new[] { "AgentVersion", "Capabilities", "CollectorVersion", "SupportedActions" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-01",
                columns: new[] { "AgentVersion", "Capabilities", "CollectorVersion", "SupportedActions" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-02",
                columns: new[] { "AgentVersion", "Capabilities", "CollectorVersion", "SupportedActions" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_response_actions_AgentId",
                table: "response_actions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_response_actions_ApprovedByUserId",
                table: "response_actions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_response_actions_IncidentId",
                table: "response_actions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_response_actions_RequestedByUserId",
                table: "response_actions",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "response_actions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "alert_rules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "alert_rules");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "alert_rules");

            migrationBuilder.DropColumn(
                name: "AgentVersion",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "CollectorVersion",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "SupportedActions",
                table: "agents");
        }
    }
}
