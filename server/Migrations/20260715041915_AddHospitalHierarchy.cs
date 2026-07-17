using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OneSecurity.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "infrastructure_assets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "agents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "hospitals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hospitals_hospitals_ParentId",
                        column: x => x.ParentId,
                        principalTable: "hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-db-01",
                column: "HospitalId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-lnx-web-02",
                column: "HospitalId",
                value: 5);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-app-01",
                column: "HospitalId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-01",
                column: "HospitalId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "agents",
                keyColumn: "Id",
                keyValue: "agent-win-his-02",
                column: "HospitalId",
                value: 5);

            migrationBuilder.InsertData(
                table: "hospitals",
                columns: new[] { "Id", "Code", "Name", "ParentId" },
                values: new object[] { 1, "ROOT", "Hospital Tổng", null });

            migrationBuilder.UpdateData(
                table: "infrastructure_assets",
                keyColumn: "Id",
                keyValue: 1L,
                column: "HospitalId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "infrastructure_assets",
                keyColumn: "Id",
                keyValue: 2L,
                column: "HospitalId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "infrastructure_assets",
                keyColumn: "Id",
                keyValue: 3L,
                column: "HospitalId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "infrastructure_assets",
                keyColumn: "Id",
                keyValue: 4L,
                column: "HospitalId",
                value: 5);

            migrationBuilder.UpdateData(
                table: "infrastructure_assets",
                keyColumn: "Id",
                keyValue: 5L,
                column: "HospitalId",
                value: 5);

            migrationBuilder.InsertData(
                table: "hospitals",
                columns: new[] { "Id", "Code", "Name", "ParentId" },
                values: new object[,]
                {
                    { 2, "HOSP_A", "Hospital A", 1 },
                    { 5, "HOSP_B", "Hospital B", 1 },
                    { 3, "HOSP_A1", "Hospital A1", 2 },
                    { 4, "HOSP_A2", "Hospital A2", 2 },
                    { 6, "HOSP_B1", "Hospital B1", 5 },
                    { 7, "HOSP_B2", "Hospital B2", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_infrastructure_assets_HospitalId",
                table: "infrastructure_assets",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_HospitalId",
                table: "AspNetUsers",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_agents_HospitalId",
                table: "agents",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitals_ParentId",
                table: "hospitals",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_agents_hospitals_HospitalId",
                table: "agents",
                column: "HospitalId",
                principalTable: "hospitals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_hospitals_HospitalId",
                table: "AspNetUsers",
                column: "HospitalId",
                principalTable: "hospitals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_infrastructure_assets_hospitals_HospitalId",
                table: "infrastructure_assets",
                column: "HospitalId",
                principalTable: "hospitals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agents_hospitals_HospitalId",
                table: "agents");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_hospitals_HospitalId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_infrastructure_assets_hospitals_HospitalId",
                table: "infrastructure_assets");

            migrationBuilder.DropTable(
                name: "hospitals");

            migrationBuilder.DropIndex(
                name: "IX_infrastructure_assets_HospitalId",
                table: "infrastructure_assets");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_HospitalId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_agents_HospitalId",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "infrastructure_assets");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "agents");
        }
    }
}
