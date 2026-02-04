using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class AddFormationVersioningAndAuditDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentsAudit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeDocument = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CheminFichier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CritereQualiopi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DateValidite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Statut = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Commentaire = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsAudit_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormationVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormationId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DateVersion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RaisonModification = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Titre = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Programme = table.Column<string>(type: "TEXT", nullable: false),
                    Prerequis = table.Column<string>(type: "TEXT", nullable: false),
                    ModalitesPedagogiques = table.Column<string>(type: "TEXT", nullable: false),
                    ModalitesEvaluation = table.Column<string>(type: "TEXT", nullable: false),
                    ReferencesQualiopi = table.Column<string>(type: "TEXT", nullable: false),
                    DureeHeures = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixIndicatif = table.Column<decimal>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormationVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormationVersions_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsAudit_FormationId",
                table: "DocumentsAudit",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsAudit_TypeDocument",
                table: "DocumentsAudit",
                column: "TypeDocument");

            migrationBuilder.CreateIndex(
                name: "IX_FormationVersions_DateVersion",
                table: "FormationVersions",
                column: "DateVersion");

            migrationBuilder.CreateIndex(
                name: "IX_FormationVersions_FormationId_NumeroVersion",
                table: "FormationVersions",
                columns: new[] { "FormationId", "NumeroVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentsAudit");

            migrationBuilder.DropTable(
                name: "FormationVersions");
        }
    }
}
