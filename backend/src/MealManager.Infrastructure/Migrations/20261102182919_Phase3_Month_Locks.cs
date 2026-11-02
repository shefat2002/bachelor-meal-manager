using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_Month_Locks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthLocks_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthLocks_MessId_Year_Month",
                table: "MonthLocks",
                columns: new[] { "MessId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthLocks");
        }
    }
}
