using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrchestAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryMaxAttempts",
                table: "Workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryBackoffMs",
                table: "Workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "RetryBackoffMultiplier",
                table: "Workflows",
                type: "double precision",
                nullable: false,
                defaultValue: 2.0);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "NodeExecutions",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RetryMaxAttempts", table: "Workflows");
            migrationBuilder.DropColumn(name: "RetryBackoffMs", table: "Workflows");
            migrationBuilder.DropColumn(name: "RetryBackoffMultiplier", table: "Workflows");
            migrationBuilder.DropColumn(name: "AttemptNumber", table: "NodeExecutions");
        }
    }
}
