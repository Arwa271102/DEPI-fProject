using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phase55And6AssignmentMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApartmentGroups_ApartmentId_GroupStatus",
                table: "ApartmentGroups");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ApartmentGroups",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "ApartmentGroups",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentGroups_ApartmentId_GroupStatus",
                table: "ApartmentGroups",
                columns: new[] { "ApartmentId", "GroupStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApartmentGroups_ApartmentId_GroupStatus",
                table: "ApartmentGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ApartmentGroups");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "ApartmentGroups");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentGroups_ApartmentId_GroupStatus",
                table: "ApartmentGroups",
                columns: new[] { "ApartmentId", "GroupStatus" },
                unique: true,
                filter: "[GroupStatus] = 'Open'");
        }
    }
}
