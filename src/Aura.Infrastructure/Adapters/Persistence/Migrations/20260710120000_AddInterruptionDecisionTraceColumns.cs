using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterruptionDecisionTraceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetrievedSemanticContext",
                table: "InterruptionDecisions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LlmRationale",
                table: "InterruptionDecisions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardrailOutcome",
                table: "InterruptionDecisions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetrievedSemanticContext",
                table: "InterruptionDecisions");

            migrationBuilder.DropColumn(
                name: "LlmRationale",
                table: "InterruptionDecisions");

            migrationBuilder.DropColumn(
                name: "GuardrailOutcome",
                table: "InterruptionDecisions");
        }
    }
}
