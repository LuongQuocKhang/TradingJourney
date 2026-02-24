using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingJournal.Modules.Trades.Migrations
{
    /// <inheritdoc />
    public partial class initdatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Trades");

            migrationBuilder.CreateTable(
                name: "EmotionTags",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PsychologyType = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmotionTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PretradeChecklists",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CheckListType = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PretradeChecklists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskGuardrails",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountEquity = table.Column<double>(type: "double precision", nullable: true),
                    RiskPercentage = table.Column<double>(type: "double precision", nullable: true),
                    MaxDailyLoss = table.Column<double>(type: "double precision", nullable: true),
                    TakeProfit = table.Column<double>(type: "double precision", nullable: true),
                    PositionSize = table.Column<double>(type: "double precision", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskGuardrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingSessions",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FromTime = table.Column<string>(type: "text", nullable: false),
                    ToTime = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeHistorys",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Asset = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    EntryPrice = table.Column<double>(type: "double precision", nullable: false),
                    TargetTier1 = table.Column<double>(type: "double precision", nullable: false),
                    TargetTier2 = table.Column<double>(type: "double precision", nullable: true),
                    TargetTier3 = table.Column<double>(type: "double precision", nullable: true),
                    StopLoss = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExitPrice = table.Column<double>(type: "double precision", nullable: true),
                    Pnl = table.Column<double>(type: "double precision", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    PsychologyNotes = table.Column<string>(type: "text", nullable: true),
                    TradingSessionId = table.Column<int>(type: "integer", nullable: true),
                    RiskGuardrailId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeHistorys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeHistorys_RiskGuardrails_RiskGuardrailId",
                        column: x => x.RiskGuardrailId,
                        principalSchema: "Trades",
                        principalTable: "RiskGuardrails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TradeHistorys_TradingSessions_TradingSessionId",
                        column: x => x.TradingSessionId,
                        principalSchema: "Trades",
                        principalTable: "TradingSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Screenshots",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    TradeHistoryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Screenshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Screenshots_TradeHistorys_TradeHistoryId",
                        column: x => x.TradeHistoryId,
                        principalSchema: "Trades",
                        principalTable: "TradeHistorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeEmotionTags",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeHistoryId = table.Column<int>(type: "integer", nullable: false),
                    EmotionTagId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeEmotionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeEmotionTags_EmotionTags_EmotionTagId",
                        column: x => x.EmotionTagId,
                        principalSchema: "Trades",
                        principalTable: "EmotionTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeEmotionTags_TradeHistorys_TradeHistoryId",
                        column: x => x.TradeHistoryId,
                        principalSchema: "Trades",
                        principalTable: "TradeHistorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeHistoryChecklists",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeHistoryId = table.Column<int>(type: "integer", nullable: false),
                    PretradeChecklistId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeHistoryChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeHistoryChecklists_PretradeChecklists_PretradeChecklist~",
                        column: x => x.PretradeChecklistId,
                        principalSchema: "Trades",
                        principalTable: "PretradeChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeHistoryChecklists_TradeHistorys_TradeHistoryId",
                        column: x => x.TradeHistoryId,
                        principalSchema: "Trades",
                        principalTable: "TradeHistorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Screenshots_TradeHistoryId",
                schema: "Trades",
                table: "Screenshots",
                column: "TradeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeEmotionTags_EmotionTagId",
                schema: "Trades",
                table: "TradeEmotionTags",
                column: "EmotionTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeEmotionTags_TradeHistoryId",
                schema: "Trades",
                table: "TradeEmotionTags",
                column: "TradeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeHistoryChecklists_PretradeChecklistId",
                schema: "Trades",
                table: "TradeHistoryChecklists",
                column: "PretradeChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeHistoryChecklists_TradeHistoryId",
                schema: "Trades",
                table: "TradeHistoryChecklists",
                column: "TradeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeHistorys_RiskGuardrailId",
                schema: "Trades",
                table: "TradeHistorys",
                column: "RiskGuardrailId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeHistorys_TradingSessionId",
                schema: "Trades",
                table: "TradeHistorys",
                column: "TradingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Screenshots",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "TradeEmotionTags",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "TradeHistoryChecklists",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "EmotionTags",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "PretradeChecklists",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "TradeHistorys",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "RiskGuardrails",
                schema: "Trades");

            migrationBuilder.DropTable(
                name: "TradingSessions",
                schema: "Trades");
        }
    }
}
