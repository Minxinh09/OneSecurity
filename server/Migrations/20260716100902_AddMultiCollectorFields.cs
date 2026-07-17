using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCollectorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConnectedAgents",
                table: "collector_nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "collector_nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "collector_nodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "QueueSize",
                table: "collector_nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "collector_nodes",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConnectedAgents", "HospitalId", "LastSeen", "QueueSize" },
                values: new object[] { 0, 0, new DateTime(2026, 7, 16, 10, 9, 1, 660, DateTimeKind.Utc).AddTicks(6322), 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectedAgents",
                table: "collector_nodes");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "collector_nodes");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "collector_nodes");

            migrationBuilder.DropColumn(
                name: "QueueSize",
                table: "collector_nodes");
        }
    }
}
