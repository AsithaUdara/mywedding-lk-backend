using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_DomainUpdate_PlannerMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventLifecycleStage",
                table: "WeddingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ManagingPlannerId",
                table: "WeddingEvents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "EventTasks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DependsOnTaskId",
                table: "EventTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "EventTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeddingEvents_ManagingPlannerId",
                table: "WeddingEvents",
                column: "ManagingPlannerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTasks_AssignedToUserId",
                table: "EventTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTasks_DependsOnTaskId",
                table: "EventTasks",
                column: "DependsOnTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTasks_EventTasks_DependsOnTaskId",
                table: "EventTasks",
                column: "DependsOnTaskId",
                principalTable: "EventTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventTasks_Users_AssignedToUserId",
                table: "EventTasks",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WeddingEvents_WeddingPlanners_ManagingPlannerId",
                table: "WeddingEvents",
                column: "ManagingPlannerId",
                principalTable: "WeddingPlanners",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTasks_EventTasks_DependsOnTaskId",
                table: "EventTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_EventTasks_Users_AssignedToUserId",
                table: "EventTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_WeddingEvents_WeddingPlanners_ManagingPlannerId",
                table: "WeddingEvents");

            migrationBuilder.DropIndex(
                name: "IX_WeddingEvents_ManagingPlannerId",
                table: "WeddingEvents");

            migrationBuilder.DropIndex(
                name: "IX_EventTasks_AssignedToUserId",
                table: "EventTasks");

            migrationBuilder.DropIndex(
                name: "IX_EventTasks_DependsOnTaskId",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "EventLifecycleStage",
                table: "WeddingEvents");

            migrationBuilder.DropColumn(
                name: "ManagingPlannerId",
                table: "WeddingEvents");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "DependsOnTaskId",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "EventTasks");
        }
    }
}
