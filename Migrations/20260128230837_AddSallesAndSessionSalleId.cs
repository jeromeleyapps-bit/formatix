using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSallesAndSessionSalleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalleId",
                table: "Sessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Salles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Capacite = table.Column<int>(type: "INTEGER", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SalleId",
                table: "Sessions",
                column: "SalleId");

            migrationBuilder.CreateIndex(
                name: "IX_Salles_Nom",
                table: "Salles",
                column: "Nom");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Salles_SalleId",
                table: "Sessions",
                column: "SalleId",
                principalTable: "Salles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Salles_SalleId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "Salles");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_SalleId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SalleId",
                table: "Sessions");
        }
    }
}
