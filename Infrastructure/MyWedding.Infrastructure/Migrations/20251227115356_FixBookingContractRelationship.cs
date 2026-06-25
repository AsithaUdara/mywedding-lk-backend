using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingContractRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingContracts_VendorBookings_Id",
                table: "BookingContracts");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingContracts_VendorBookings_Id",
                table: "BookingContracts",
                column: "Id",
                principalTable: "VendorBookings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingContracts_VendorBookings_Id",
                table: "BookingContracts");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingContracts_VendorBookings_Id",
                table: "BookingContracts",
                column: "Id",
                principalTable: "VendorBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
