using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phasetwolast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousValues",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "ApartmentUpload");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Landlords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationRequestedAt",
                table: "Landlords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Landlords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedByAdminId",
                table: "Landlords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Landlords_VerificationRequestedAt",
                table: "Landlords",
                column: "VerificationRequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Landlords_VerifiedByAdminId",
                table: "Landlords",
                column: "VerifiedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Landlords_Admins_VerifiedByAdminId",
                table: "Landlords",
                column: "VerifiedByAdminId",
                principalTable: "Admins",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Landlords_Admins_VerifiedByAdminId",
                table: "Landlords");

            migrationBuilder.DropIndex(
                name: "IX_Landlords_VerificationRequestedAt",
                table: "Landlords");

            migrationBuilder.DropIndex(
                name: "IX_Landlords_VerifiedByAdminId",
                table: "Landlords");

            migrationBuilder.DropColumn(
                name: "PreviousValues",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Landlords");

            migrationBuilder.DropColumn(
                name: "VerificationRequestedAt",
                table: "Landlords");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Landlords");

            migrationBuilder.DropColumn(
                name: "VerifiedByAdminId",
                table: "Landlords");
        }
    }
}
