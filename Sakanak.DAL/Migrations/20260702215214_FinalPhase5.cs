using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FinalPhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Bookings");
        }
    }
}
