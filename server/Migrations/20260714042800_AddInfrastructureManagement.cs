using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddInfrastructureManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AssetId",
                table: "agents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_policies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    HeartbeatInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricsInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    EnabledLogs = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "collector_nodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", nullable: false),
                    CollectorKey = table.Column<string>(type: "TEXT", nullable: false),
                    SharedSecret = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSync = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfigurationVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    RulesVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collector_nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "infrastructure_assets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", nullable: false),
                    OperatingSystem = table.Column<string>(type: "TEXT", nullable: false),
                    OperatingSystemVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    Building = table.Column<string>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Criticality = table.Column<string>(type: "TEXT", nullable: false),
                    AssetType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CollectorId = table.Column<long>(type: "INTEGER", nullable: false),
                    PolicyId = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_infrastructure_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_infrastructure_assets_agent_policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "agent_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_infrastructure_assets_collector_nodes_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "collector_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollment_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    AssetId = table.Column<long>(type: "INTEGER", nullable: false),
                    PolicyId = table.Column<long>(type: "INTEGER", nullable: false),
                    CollectorId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Used = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxUses = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollment_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enrollment_tokens_agent_policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "agent_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollment_tokens_collector_nodes_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "collector_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollment_tokens_infrastructure_assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "infrastructure_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "agent_policies",
                columns: new[] { "Id", "Description", "EnabledLogs", "HeartbeatInterval", "MetricsInterval", "Name", "ResponseEnabled", "Version" },
                values: new object[] { 1L, "Standard monitor policy with responses enabled.", "ProcessMonitor,FileIntegrity,NetworkMonitor", 10, 10, "Default Policy", true, 1 });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-db-01",
                column: "AssetId",
                value: 2L);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-web-02",
                column: "AssetId",
                value: 5L);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-app-01",
                column: "AssetId",
                value: 3L);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-01",
                column: "AssetId",
                value: 1L);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-02",
                column: "AssetId",
                value: 4L);

            migrationBuilder.InsertData(
                table: "collector_nodes",
                columns: new[] { "Id", "CollectorKey", "ConfigurationVersion", "Description", "IPAddress", "LastHeartbeat", "LastSync", "Location", "Name", "RulesVersion", "SharedSecret", "Status", "Version" },
                values: new object[] { 1L, "primary_key", 1, "Primary SOC Collector Node", "127.0.0.1", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "HQ Data Center", "Primary Collector", 1, "32ac234d7a942bdbe88387389da17e11b2cbb577ee2d8c17d5841705a8629724", "Online", "1.2.0" });

            migrationBuilder.InsertData(
                table: "infrastructure_assets",
                columns: new[] { "Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "Server", "Main Building", 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "High", "IT", "HIS Web Server", "WORKGROUP", "DESKTOP-FC9F6G9", "192.168.1.35", null, "Windows", "Server 2022", "sysadmin", 1L, "Managed", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, "Server", "Server Room A", 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Critical", "Security", "Main Database Server", "hospa.local", "ubuntu-hosp-a-db", "192.168.1.40", null, "Linux", "Ubuntu 22.04 LTS", "dbadmin", 1L, "Managed", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, "Server", "Server Room A", 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "High", "IT", "Application Gateway", "hospa.local", "win-hosp-a-app", "192.168.1.41", null, "Windows", "Server 2019", "sysadmin", 1L, "Managed", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, "Server", "Clinic B", 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "High", "Medical", "Clinic Portal Host", "hospb.local", "win-hosp-b-his", "192.168.1.50", null, "Windows", "Server 2022", "clinicadmin", 1L, "Managed", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, "Server", "Clinic B", 1L, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Medium", "Public Relations", "Public Website Host", "hospb.local", "ubuntu-hosp-b-web", "192.168.1.51", null, "Linux", "Ubuntu 20.04 LTS", "webadmin", 1L, "Managed", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_agents_AssetId",
                table: "agents",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_collector_nodes_CollectorKey",
                table: "collector_nodes",
                column: "CollectorKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_tokens_AssetId",
                table: "enrollment_tokens",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_tokens_CollectorId",
                table: "enrollment_tokens",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_tokens_PolicyId",
                table: "enrollment_tokens",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_tokens_Token",
                table: "enrollment_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_infrastructure_assets_CollectorId",
                table: "infrastructure_assets",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_infrastructure_assets_Hostname",
                table: "infrastructure_assets",
                column: "Hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_infrastructure_assets_PolicyId",
                table: "infrastructure_assets",
                column: "PolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_agents_infrastructure_assets_AssetId",
                table: "agents",
                column: "AssetId",
                principalTable: "infrastructure_assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agents_infrastructure_assets_AssetId",
                table: "agents");

            migrationBuilder.DropTable(
                name: "enrollment_tokens");

            migrationBuilder.DropTable(
                name: "infrastructure_assets");

            migrationBuilder.DropTable(
                name: "agent_policies");

            migrationBuilder.DropTable(
                name: "collector_nodes");

            migrationBuilder.DropIndex(
                name: "IX_agents_AssetId",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "agents");
        }
    }
}
