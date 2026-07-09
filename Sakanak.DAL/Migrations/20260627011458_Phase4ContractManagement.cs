using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ContractManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "Contracts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Contracts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Contracts");
        }
    }
}
