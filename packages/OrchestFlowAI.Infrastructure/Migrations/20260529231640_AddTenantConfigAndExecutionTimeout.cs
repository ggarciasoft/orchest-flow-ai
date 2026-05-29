using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrchestFlowAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantConfigAndExecutionTimeout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "WorkflowExecutions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "WorkflowExecutions");
        }
    }
}
