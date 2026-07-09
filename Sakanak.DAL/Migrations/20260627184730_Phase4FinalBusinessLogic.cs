using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phase4FinalBusinessLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressAtBooking",
                table: "Bookings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AmenitiesAtBooking",
                table: "Bookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CityAtBooking",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonthAtBooking",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressAtBooking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AmenitiesAtBooking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CityAtBooking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PricePerMonthAtBooking",
                table: "Bookings");
        }
    }
}
