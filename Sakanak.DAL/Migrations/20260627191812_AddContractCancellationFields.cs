using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddContractCancellationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Contracts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByAdminId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CancelledByAdminId",
                table: "Contracts",
                column: "CancelledByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Admins_CancelledByAdminId",
                table: "Contracts",
                column: "CancelledByAdminId",
                principalTable: "Admins",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Admins_CancelledByAdminId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_CancelledByAdminId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CancelledByAdminId",
                table: "Contracts");
        }
    }
}
