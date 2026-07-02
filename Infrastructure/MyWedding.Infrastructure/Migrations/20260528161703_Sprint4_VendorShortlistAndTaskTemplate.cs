using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_VendorShortlistAndTaskTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorShortlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProposedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VendorBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentToClientAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorShortlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorShortlistItems_VendorBookings_VendorBookingId",
                        column: x => x.VendorBookingId,
                        principalTable: "VendorBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorShortlistItems_VendorServices_VendorServiceId",
                        column: x => x.VendorServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorShortlistItems_WeddingEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "WeddingEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorShortlistItems_EventId_VendorServiceId",
                table: "VendorShortlistItems",
                columns: new[] { "EventId", "VendorServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorShortlistItems_VendorBookingId",
                table: "VendorShortlistItems",
                column: "VendorBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorShortlistItems_VendorServiceId",
                table: "VendorShortlistItems",
                column: "VendorServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorShortlistItems");
        }
    }
}
