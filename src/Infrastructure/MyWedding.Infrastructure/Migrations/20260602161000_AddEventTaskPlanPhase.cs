using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWedding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTaskPlanPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskPlanPhase",
                table: "WeddingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE e
                SET TaskPlanPhase = 2
                FROM WeddingEvents e
                INNER JOIN (
                    SELECT EventId
                    FROM EventTasks
                    GROUP BY EventId
                    HAVING COUNT(*) >= 40
                ) t ON e.Id = t.EventId;
                """);

            migrationBuilder.Sql(
                """
                UPDATE e
                SET TaskPlanPhase = 1
                FROM WeddingEvents e
                INNER JOIN (
                    SELECT EventId
                    FROM EventTasks
                    GROUP BY EventId
                    HAVING COUNT(*) > 0 AND COUNT(*) < 40
                ) t ON e.Id = t.EventId
                WHERE e.TaskPlanPhase = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskPlanPhase",
                table: "WeddingEvents");
        }
    }
}
