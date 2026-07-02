using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPlanningBrief : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedGuestCount",
                table: "WeddingEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuestCountMax",
                table: "WeddingEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeddingStyle",
                table: "WeddingEvents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenuePreference",
                table: "WeddingEvents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MustHavesNotes",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServicesAlreadyBooked",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CulturalOrReligiousNotes",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BriefCompletedAt",
                table: "WeddingEvents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EstimatedGuestCount", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "GuestCountMax", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "WeddingStyle", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "VenuePreference", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "MustHavesNotes", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "ServicesAlreadyBooked", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "CulturalOrReligiousNotes", table: "WeddingEvents");
            migrationBuilder.DropColumn(name: "BriefCompletedAt", table: "WeddingEvents");
        }
    }
}
