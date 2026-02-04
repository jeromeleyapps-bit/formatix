using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class CreateFormateurEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionsVeille_AspNetUsers_FormateurId",
                table: "ActionsVeille");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_AspNetUsers_FormateurId",
                table: "Sessions");

            migrationBuilder.CreateTable(
                name: "Formateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilisateurId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StatutProfessionnel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NumeroFormateur = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AntenneRattachement = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Biographie = table.Column<string>(type: "TEXT", nullable: false),
                    Competences = table.Column<string>(type: "TEXT", nullable: false),
                    Experience = table.Column<string>(type: "TEXT", nullable: false),
                    Formations = table.Column<string>(type: "TEXT", nullable: false),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Formateurs_AspNetUsers_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Formateurs_Actif",
                table: "Formateurs",
                column: "Actif");

            migrationBuilder.CreateIndex(
                name: "IX_Formateurs_Email",
                table: "Formateurs",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Formateurs_NumeroFormateur",
                table: "Formateurs",
                column: "NumeroFormateur");

            migrationBuilder.CreateIndex(
                name: "IX_Formateurs_UtilisateurId",
                table: "Formateurs",
                column: "UtilisateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionsVeille_Formateurs_FormateurId",
                table: "ActionsVeille",
                column: "FormateurId",
                principalTable: "Formateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Formateurs_FormateurId",
                table: "Sessions",
                column: "FormateurId",
                principalTable: "Formateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionsVeille_Formateurs_FormateurId",
                table: "ActionsVeille");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Formateurs_FormateurId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "Formateurs");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionsVeille_AspNetUsers_FormateurId",
                table: "ActionsVeille",
                column: "FormateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_AspNetUsers_FormateurId",
                table: "Sessions",
                column: "FormateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
