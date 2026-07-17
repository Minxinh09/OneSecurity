using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class RefineAuditLogsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_CorrelationId",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "audit_logs",
                newName: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Severity",
                table: "audit_logs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Success",
                table: "audit_logs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_TimestampUtc_Severity",
                table: "audit_logs",
                columns: new[] { "TimestampUtc", "Severity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_Severity",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_Success",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_TimestampUtc_Severity",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "Roles",
                table: "audit_logs",
                newName: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CorrelationId",
                table: "audit_logs",
                column: "CorrelationId");
        }
    }
}
