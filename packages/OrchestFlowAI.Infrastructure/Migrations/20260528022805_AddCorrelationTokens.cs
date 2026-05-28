using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrchestFlowAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorrelationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrelationTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorrelationTokens_ExecutionId",
                table: "CorrelationTokens",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrelationTokens_Token",
                table: "CorrelationTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrelationTokens");
        }
    }
}
