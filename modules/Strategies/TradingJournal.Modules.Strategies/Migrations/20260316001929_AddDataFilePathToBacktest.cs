using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Modules.Strategies.Migrations
{
    /// <inheritdoc />
    public partial class AddDataFilePathToBacktest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataFilePath",
                schema: "Strategies",
                table: "Backtests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFilePath",
                schema: "Strategies",
                table: "Backtests");
        }
    }
}
