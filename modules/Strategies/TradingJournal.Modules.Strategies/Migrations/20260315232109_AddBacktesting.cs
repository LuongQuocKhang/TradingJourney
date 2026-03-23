using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Modules.Strategies.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Backtests",
                schema: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialCapital = table.Column<double>(type: "float", nullable: false),
                    FinalCapital = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    WinCount = table.Column<int>(type: "int", nullable: false),
                    LossCount = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<double>(type: "float", nullable: false),
                    TotalPnl = table.Column<double>(type: "float", nullable: false),
                    ProfitFactor = table.Column<double>(type: "float", nullable: false),
                    MaxDrawdown = table.Column<double>(type: "float", nullable: false),
                    MaxDrawdownPct = table.Column<double>(type: "float", nullable: false),
                    SharpeRatio = table.Column<double>(type: "float", nullable: false),
                    AvgWin = table.Column<double>(type: "float", nullable: false),
                    AvgLoss = table.Column<double>(type: "float", nullable: false),
                    LargestWin = table.Column<double>(type: "float", nullable: false),
                    LargestLoss = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backtests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Backtests_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalSchema: "Strategies",
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BacktestTrades",
                schema: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacktestId = table.Column<int>(type: "int", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    EntryPrice = table.Column<double>(type: "float", nullable: false),
                    ExitPrice = table.Column<double>(type: "float", nullable: false),
                    StopLoss = table.Column<double>(type: "float", nullable: false),
                    TakeProfit = table.Column<double>(type: "float", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Pnl = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestTrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacktestTrades_Backtests_BacktestId",
                        column: x => x.BacktestId,
                        principalSchema: "Strategies",
                        principalTable: "Backtests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Backtests_StrategyId",
                schema: "Strategies",
                table: "Backtests",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestTrades_BacktestId",
                schema: "Strategies",
                table: "BacktestTrades",
                column: "BacktestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacktestTrades",
                schema: "Strategies");

            migrationBuilder.DropTable(
                name: "Backtests",
                schema: "Strategies");
        }
    }
}
