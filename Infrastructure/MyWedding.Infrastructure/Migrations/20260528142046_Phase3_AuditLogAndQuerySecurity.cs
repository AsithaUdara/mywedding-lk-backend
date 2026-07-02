using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_AuditLogAndQuerySecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityFeedItems_Users_UserId",
                table: "ActivityFeedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityFeedItems_WeddingEvents_EventId",
                table: "ActivityFeedItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityFeedItems",
                table: "ActivityFeedItems");

            migrationBuilder.RenameTable(
                name: "ActivityFeedItems",
                newName: "AuditLogItems");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AuditLogItems",
                newName: "ActorId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AuditLogItems",
                newName: "TimestampUtc");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityFeedItems_UserId",
                table: "AuditLogItems",
                newName: "IX_AuditLogItems_ActorId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityFeedItems_EventId",
                table: "AuditLogItems",
                newName: "IX_AuditLogItems_EventId");

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "AuditLogItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "AuditLogItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE AuditLogItems
                SET ActionType = CASE
                    WHEN ItemType = 1 THEN 'UserComment'
                    ELSE 'SystemLog'
                END
                """
            );

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "AuditLogItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogItems",
                table: "AuditLogItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogItems_Users_ActorId",
                table: "AuditLogItems",
                column: "ActorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogItems_WeddingEvents_EventId",
                table: "AuditLogItems",
                column: "EventId",
                principalTable: "WeddingEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogItems_Users_ActorId",
                table: "AuditLogItems");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogItems_WeddingEvents_EventId",
                table: "AuditLogItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogItems",
                table: "AuditLogItems");

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "AuditLogItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE AuditLogItems
                SET ItemType = CASE
                    WHEN ActionType = 'UserComment' THEN 1
                    ELSE 0
                END
                """
            );

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "AuditLogItems");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "AuditLogItems");

            migrationBuilder.RenameColumn(
                name: "ActorId",
                table: "AuditLogItems",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "AuditLogItems",
                newName: "CreatedAt");

            migrationBuilder.RenameTable(
                name: "AuditLogItems",
                newName: "ActivityFeedItems");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogItems_EventId",
                table: "ActivityFeedItems",
                newName: "IX_ActivityFeedItems_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogItems_ActorId",
                table: "ActivityFeedItems",
                newName: "IX_ActivityFeedItems_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityFeedItems",
                table: "ActivityFeedItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityFeedItems_Users_UserId",
                table: "ActivityFeedItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityFeedItems_WeddingEvents_EventId",
                table: "ActivityFeedItems",
                column: "EventId",
                principalTable: "WeddingEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
