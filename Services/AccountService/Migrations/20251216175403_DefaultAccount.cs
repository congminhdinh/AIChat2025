using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class DefaultAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "Xgz3816ok9rbQwhcSCYt00NH9qkEvWDdWiY9LH6fZy4=:ae98995e673341afb9f1932ec28c2c90");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "7Qp+0J3/1Zc/u/O8T/uFzO/o6uX/iT4/5z/0q/3z/q8=:b6a9876543210fedcba9876543210fed");
        }
    }
}
