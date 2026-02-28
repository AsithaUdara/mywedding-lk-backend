using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultIsActiveToTrue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set all existing services to Active
            migrationBuilder.Sql("UPDATE VendorServices SET IsActive = 1");

            // Change default value to true (1) for future inserts
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorServices",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorServices",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
