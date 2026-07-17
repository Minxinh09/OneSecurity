using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseActionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "response_actions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "response_actions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "response_actions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Output",
                table: "response_actions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Parameters",
                table: "response_actions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "collector_nodes",
                keyColumn: "Id",
                keyValue: 1L,
                column: "LastSeen",
                value: new DateTime(2026, 7, 17, 9, 10, 34, 191, DateTimeKind.Utc).AddTicks(8516));

            migrationBuilder.CreateIndex(
                name: "IX_response_actions_HospitalId",
                table: "response_actions",
                column: "HospitalId");

            migrationBuilder.AddForeignKey(
                name: "FK_response_actions_hospitals_HospitalId",
                table: "response_actions",
                column: "HospitalId",
                principalTable: "hospitals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_response_actions_hospitals_HospitalId",
                table: "response_actions");

            migrationBuilder.DropIndex(
                name: "IX_response_actions_HospitalId",
                table: "response_actions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "response_actions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "response_actions");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "response_actions");

            migrationBuilder.DropColumn(
                name: "Output",
                table: "response_actions");

            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "response_actions");

            migrationBuilder.UpdateData(
                table: "collector_nodes",
                keyColumn: "Id",
                keyValue: 1L,
                column: "LastSeen",
                value: new DateTime(2026, 7, 16, 10, 9, 1, 660, DateTimeKind.Utc).AddTicks(6322));
        }
    }
}
