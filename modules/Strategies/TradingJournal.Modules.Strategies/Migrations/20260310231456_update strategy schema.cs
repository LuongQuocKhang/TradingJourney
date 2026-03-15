using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Modules.Strategies.Migrations
{
    /// <inheritdoc />
    public partial class updatestrategyschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Strategies");

            migrationBuilder.CreateTable(
                name: "Strategies",
                schema: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timeframe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRangeStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntryIndicators = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitIndicators = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskPerTrade = table.Column<double>(type: "float", nullable: false),
                    StopLossType = table.Column<int>(type: "int", nullable: false),
                    StopLossValue = table.Column<double>(type: "float", nullable: false),
                    TakeProfitType = table.Column<int>(type: "int", nullable: false),
                    TakeProfitValue = table.Column<double>(type: "float", nullable: false),
                    PositionSizing = table.Column<int>(type: "int", nullable: false),
                    PositionSizeValue = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyTemplates",
                schema: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timeframe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRangeStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntryIndicators = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitIndicators = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskPerTrade = table.Column<double>(type: "float", nullable: false),
                    StopLossType = table.Column<int>(type: "int", nullable: false),
                    StopLossValue = table.Column<double>(type: "float", nullable: false),
                    TakeProfitType = table.Column<int>(type: "int", nullable: false),
                    TakeProfitValue = table.Column<double>(type: "float", nullable: false),
                    PositionSizing = table.Column<int>(type: "int", nullable: false),
                    PositionSizeValue = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Strategies",
                schema: "Strategies");

            migrationBuilder.DropTable(
                name: "StrategyTemplates",
                schema: "Strategies");
        }
    }
}
