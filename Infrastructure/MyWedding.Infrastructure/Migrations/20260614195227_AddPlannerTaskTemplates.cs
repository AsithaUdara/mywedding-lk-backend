using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannerTaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannerTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduleMode = table.Column<int>(type: "int", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannerTaskTemplates_WeddingEvents_SourceEventId",
                        column: x => x.SourceEventId,
                        principalTable: "WeddingEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlannerTaskTemplates_WeddingPlanners_PlannerId",
                        column: x => x.PlannerId,
                        principalTable: "WeddingPlanners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTaskTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StartOffsetDays = table.Column<int>(type: "int", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "int", nullable: false),
                    DependsOnSortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTaskTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannerTaskTemplateItems_PlannerTaskTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PlannerTaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTaskTemplateItems_TemplateId_SortOrder",
                table: "PlannerTaskTemplateItems",
                columns: new[] { "TemplateId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTaskTemplates_PlannerId_Name",
                table: "PlannerTaskTemplates",
                columns: new[] { "PlannerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTaskTemplates_SourceEventId",
                table: "PlannerTaskTemplates",
                column: "SourceEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannerTaskTemplateItems");

            migrationBuilder.DropTable(
                name: "PlannerTaskTemplates");
        }
    }
}
