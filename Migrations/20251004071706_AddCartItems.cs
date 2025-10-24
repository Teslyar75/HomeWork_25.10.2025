using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP_421.Migrations
{
    /// <inheritdoc />
    public partial class AddCartItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RequestPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UserLogin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VisitTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ConfirmationCode",
                table: "Visits",
                column: "ConfirmationCode");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_IsConfirmed",
                table: "Visits",
                column: "IsConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_RequestPath",
                table: "Visits",
                column: "RequestPath");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_UserLogin",
                table: "Visits",
                column: "UserLogin");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VisitTime",
                table: "Visits",
                column: "VisitTime");
        }
    }
}
