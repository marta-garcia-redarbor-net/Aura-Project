using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    AddedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    RuleType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "FocusStateOverrides",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusStateOverrides", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "InterruptionDecisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    PriorityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<string>(type: "TEXT", nullable: false),
                    FocusState = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterruptionDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAlerts",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "TEXT", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    LocalDate = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    StartsAtUtc = table.Column<string>(type: "TEXT", nullable: false),
                    JoinUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    HasBeenSent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAlerts", x => new { x.EventId, x.Trigger, x.LocalDate });
                });

            migrationBuilder.CreateTable(
                name: "MorningSummaryEmission",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LocalDate = table.Column<string>(type: "TEXT", nullable: false),
                    EmittedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorningSummaryEmission", x => new { x.UserId, x.LocalDate });
                });

            migrationBuilder.CreateTable(
                name: "MsalTokenCache",
                columns: table => new
                {
                    CacheKey = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MsalTokenCache", x => x.CacheKey);
                });

            migrationBuilder.CreateTable(
                name: "NotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkItemId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<double>(type: "REAL", nullable: false),
                    TriggerRule = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DispatchedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", nullable: true),
                    TargetUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RuleResults = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemanticOutbox",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CanonicalSourceId = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Collection = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    Processed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    CapturedAtUtc = table.Column<string>(type: "TEXT", nullable: false),
                    SchemaVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: true),
                    FaultReason = table.Column<string>(type: "TEXT", nullable: true),
                    PriorityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    OwnerUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_RuleType",
                table: "AlertRules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_InterruptionDecisions_Timestamp",
                table: "InterruptionDecisions",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_Pending",
                table: "NotificationOutbox",
                column: "DispatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SemanticOutbox_Pending",
                table: "SemanticOutbox",
                columns: new[] { "Processed", "CreatedAt" },
                filter: "[Processed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_CapturedAtUtc",
                table: "WorkItems",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ExternalId",
                table: "WorkItems",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "FocusStateOverrides");

            migrationBuilder.DropTable(
                name: "InterruptionDecisions");

            migrationBuilder.DropTable(
                name: "MeetingAlerts");

            migrationBuilder.DropTable(
                name: "MorningSummaryEmission");

            migrationBuilder.DropTable(
                name: "MsalTokenCache");

            migrationBuilder.DropTable(
                name: "NotificationOutbox");

            migrationBuilder.DropTable(
                name: "SemanticOutbox");

            migrationBuilder.DropTable(
                name: "WorkItems");
        }
    }
}
