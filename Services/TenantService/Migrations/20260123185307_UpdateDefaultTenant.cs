using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantKey",
                value: "SUPERADMIN-KEY-2025");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantKey",
                value: null);
        }
    }
}
