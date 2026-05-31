using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannerBillingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannerBillingProfiles",
                columns: table => new
                {
                    PlannerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CardholderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardBrand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Last4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryMonth = table.Column<byte>(type: "tinyint", nullable: true),
                    ExpiryYear = table.Column<short>(type: "smallint", nullable: true),
                    PayHerePaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerBillingProfiles", x => x.PlannerId);
                    table.ForeignKey(
                        name: "FK_PlannerBillingProfiles_WeddingPlanners_PlannerId",
                        column: x => x.PlannerId,
                        principalTable: "WeddingPlanners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannerBillingProfiles");
        }
    }
}
