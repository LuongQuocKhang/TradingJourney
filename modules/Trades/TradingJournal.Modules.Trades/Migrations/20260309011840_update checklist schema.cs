using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Modules.Trades.Migrations
{
    /// <inheritdoc />
    public partial class updatechecklistschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ChecklistModels",
                schema: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PretradeChecklists_ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists",
                column: "ChecklistModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_PretradeChecklists_ChecklistModels_ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists",
                column: "ChecklistModelId",
                principalSchema: "Trades",
                principalTable: "ChecklistModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PretradeChecklists_ChecklistModels_ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists");

            migrationBuilder.DropTable(
                name: "ChecklistModels",
                schema: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_PretradeChecklists_ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists");

            migrationBuilder.DropColumn(
                name: "ChecklistModelId",
                schema: "Trades",
                table: "PretradeChecklists");
        }
    }
}
