using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sakanak.DAL.Migrations
{
    /// <inheritdoc />
    public partial class MediaRefactoredd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Apartments_ApartmentId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Contracts_ContractId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Landlords_LandlordId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Students_StudentId",
                table: "Media");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Apartments_ApartmentId",
                table: "Media",
                column: "ApartmentId",
                principalTable: "Apartments",
                principalColumn: "ApartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Contracts_ContractId",
                table: "Media",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Landlords_LandlordId",
                table: "Media",
                column: "LandlordId",
                principalTable: "Landlords",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Students_StudentId",
                table: "Media",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Apartments_ApartmentId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Contracts_ContractId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Landlords_LandlordId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Students_StudentId",
                table: "Media");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Apartments_ApartmentId",
                table: "Media",
                column: "ApartmentId",
                principalTable: "Apartments",
                principalColumn: "ApartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Contracts_ContractId",
                table: "Media",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Landlords_LandlordId",
                table: "Media",
                column: "LandlordId",
                principalTable: "Landlords",
                principalColumn: "LandlordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Students_StudentId",
                table: "Media",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId");
        }
    }
}
