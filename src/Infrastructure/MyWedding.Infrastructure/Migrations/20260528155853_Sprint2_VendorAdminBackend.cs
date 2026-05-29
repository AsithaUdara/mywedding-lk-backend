using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2_VendorAdminBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "VendorInquiries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "VendorInquiries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendorBlockedDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBlockedDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBlockedDates_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorInquiryQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuoteReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PdfStorageKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorInquiryQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorInquiryQuotes_VendorInquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalTable: "VendorInquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorProfileViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorProfileViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorProfileViews_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorInquiries_EventId",
                table: "VendorInquiries",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBlockedDates_VendorId_Date",
                table: "VendorBlockedDates",
                columns: new[] { "VendorId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorInquiryQuotes_InquiryId",
                table: "VendorInquiryQuotes",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProfileViews_VendorId_ViewedAt",
                table: "VendorProfileViews",
                columns: new[] { "VendorId", "ViewedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_VendorInquiries_WeddingEvents_EventId",
                table: "VendorInquiries",
                column: "EventId",
                principalTable: "WeddingEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorInquiries_WeddingEvents_EventId",
                table: "VendorInquiries");

            migrationBuilder.DropTable(
                name: "VendorBlockedDates");

            migrationBuilder.DropTable(
                name: "VendorInquiryQuotes");

            migrationBuilder.DropTable(
                name: "VendorProfileViews");

            migrationBuilder.DropIndex(
                name: "IX_VendorInquiries_EventId",
                table: "VendorInquiries");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "VendorInquiries");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "VendorInquiries");
        }
    }
}
