using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_configs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HeartbeatIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricsIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoredLogsConfig = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: true),
                    ConditionExpression = table.Column<string>(type: "TEXT", nullable: false),
                    AlertSeverity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TelegramChatId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    OsInfo = table.Column<string>(type: "TEXT", nullable: false),
                    HardwareSpecs = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfigId = table.Column<long>(type: "INTEGER", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agents_agent_configs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "agent_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metric_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CpuUsagePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RamUsagePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DiskUsagePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    NetworkInBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkOutBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metric_records_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    RawData = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_security_events_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    RuleId = table.Column<long>(type: "INTEGER", nullable: true),
                    TriggerEventId = table.Column<long>(type: "INTEGER", nullable: true),
                    RuleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "TEXT", nullable: true),
                    TelegramSent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alerts_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_alerts_alert_rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "alert_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_alerts_security_events_TriggerEventId",
                        column: x => x.TriggerEventId,
                        principalTable: "security_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "agent_configs",
                columns: new[] { "Id", "CreatedAt", "HeartbeatIntervalSeconds", "IsDefault", "MetricsIntervalSeconds", "MonitoredLogsConfig", "Name", "UpdatedAt", "Version" },
                values: new object[] { 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, 30, "[]", "Default Hospital Policy", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.InsertData(
                table: "agents",
                columns: new[] { "Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status" },
                values: new object[,]
                {
                    { "agent-lnx-db-01", 1L, null, "ubuntu-hosp-a-db", "192.168.1.40", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Ubuntu 22.04 LTS", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "online" },
                    { "agent-lnx-web-02", 1L, null, "ubuntu-hosp-b-web", "192.168.1.51", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Ubuntu 20.04 LTS", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "online" },
                    { "agent-win-app-01", 1L, null, "win-hosp-a-app", "192.168.1.41", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Windows Server 2019", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "online" },
                    { "agent-win-his-01", 1L, null, "DESKTOP-FC9F6G9", "192.168.1.35", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Windows Server 2022", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "online" },
                    { "agent-win-his-02", 1L, null, "win-hosp-b-his", "192.168.1.50", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Windows Server 2022", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "online" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_configs_Name",
                table: "agent_configs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agents_ConfigId",
                table: "agents",
                column: "ConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_agents_LastSeenAt",
                table: "agents",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_agents_Status",
                table: "agents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_IsEnabled_EventType",
                table: "alert_rules",
                columns: new[] { "IsEnabled", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_Name",
                table: "alert_rules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alerts_AgentId",
                table: "alerts",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_CreatedAt",
                table: "alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_RuleId",
                table: "alerts",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_TriggerEventId",
                table: "alerts",
                column: "TriggerEventId");

            migrationBuilder.CreateIndex(
                name: "IX_metric_records_agent_timestamp_desc",
                table: "metric_records",
                columns: new[] { "AgentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_records_Timestamp",
                table: "metric_records",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_security_events_agent_timestamp_desc",
                table: "security_events",
                columns: new[] { "AgentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_security_events_EventId",
                table: "security_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_events_Severity",
                table: "security_events",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_security_events_Timestamp",
                table: "security_events",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "metric_records");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "security_events");

            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "agent_configs");
        }
    }
}
