using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_Meals_Bazar_Utilities_Deposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BazarExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BazarExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BazarExpenses_AspNetUsers_BuyerMemberId",
                        column: x => x.BuyerMemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BazarExpenses_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyMeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Breakfast = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    Lunch = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    Dinner = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyMeals_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyMeals_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deposits_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deposits_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilityTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityTypes_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilityPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityPayments_Messes_MessId",
                        column: x => x.MessId,
                        principalTable: "Messes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtilityPayments_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BazarExpenses_BuyerMemberId",
                table: "BazarExpenses",
                column: "BuyerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BazarExpenses_MessId_Date",
                table: "BazarExpenses",
                columns: new[] { "MessId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMeals_MemberId",
                table: "DailyMeals",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMeals_MessId_Date",
                table: "DailyMeals",
                columns: new[] { "MessId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMeals_MessId_MemberId_Date",
                table: "DailyMeals",
                columns: new[] { "MessId", "MemberId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_MemberId",
                table: "Deposits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_MessId_Date",
                table: "Deposits",
                columns: new[] { "MessId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityPayments_MessId_PaymentDate",
                table: "UtilityPayments",
                columns: new[] { "MessId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityPayments_UtilityTypeId",
                table: "UtilityPayments",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityTypes_MessId_Name",
                table: "UtilityTypes",
                columns: new[] { "MessId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BazarExpenses");

            migrationBuilder.DropTable(
                name: "DailyMeals");

            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropTable(
                name: "UtilityPayments");

            migrationBuilder.DropTable(
                name: "UtilityTypes");
        }
    }
}
