using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class QualiopiIndicatorsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "1", "Information du public", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CodeIndicateur", "Libelle" },
                values: new object[] { "2", "Indicateurs de résultats" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "3", "Taux d'obtention des certifications", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "4", "Analyse du besoin", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CodeIndicateur", "Libelle" },
                values: new object[] { "5", "Objectifs de la prestation" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "6", 2, "Contenus et modalités", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "7", 2, "Contenus et exigences" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "8", 2, "Positionnement à l'entrée" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "9", 3, "Conditions de déroulement", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "10", 3, "Adaptation de la prestation" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "11", 3, "Atteinte des objectifs" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "12", 3, "Engagement des bénéficiaires" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "13", 3, "Coordination des apprentis", 1 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "14", 3, "Exercice de la citoyenneté", 1 });

            migrationBuilder.InsertData(
                table: "IndicateursQualiopi",
                columns: new[] { "Id", "CodeIndicateur", "CreePar", "Critere", "DateCreation", "DateModification", "Description", "Libelle", "ModifiePar", "NiveauPreuveRequis", "SiteId" },
                values: new object[,]
                {
                    { 15, "15", "system", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Droits et devoirs de l'apprenti", "system", 1, "SITE_01" },
                    { 16, "16", "system", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Présentation à la certification", "system", 1, "SITE_01" },
                    { 17, "17", "system", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Moyens humains et techniques", "system", 1, "SITE_01" },
                    { 18, "18", "system", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Coordination des acteurs", "system", 1, "SITE_01" },
                    { 19, "19", "system", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Ressources pédagogiques", "system", 1, "SITE_01" },
                    { 20, "20", "system", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Personnels dédiés", "system", 1, "SITE_01" },
                    { 21, "21", "system", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Compétences des acteurs", "system", 1, "SITE_01" },
                    { 22, "22", "system", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Gestion de la compétence", "system", 1, "SITE_01" },
                    { 23, "23", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Veille légale et réglementaire", "system", 1, "SITE_01" },
                    { 24, "24", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Veille des emplois et métiers", "system", 1, "SITE_01" },
                    { 25, "25", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Veille pédagogique et technologique", "system", 1, "SITE_01" },
                    { 26, "26", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Situation de handicap", "system", 1, "SITE_01" },
                    { 27, "27", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Disposition sous-traitance", "system", 1, "SITE_01" },
                    { 28, "28", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Formation en situation de travail", "system", 1, "SITE_01" },
                    { 29, "29", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Insertion professionnelle", "system", 1, "SITE_01" },
                    { 30, "30", "system", 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Recueil des appréciations", "system", 1, "SITE_01" },
                    { 31, "31", "system", 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Traitement des réclamations", "system", 1, "SITE_01" },
                    { 32, "32", "system", 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Amélioration continue", "system", 1, "SITE_01" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "1.1", "Information préalable sur les objectifs, durée, contenu", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CodeIndicateur", "Libelle" },
                values: new object[] { "1.2", "Modalités d'inscription et délais" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "1.3", "Tarifs et modalités de paiement", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CodeIndicateur", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "2.1", "Adaptation aux besoins du public", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CodeIndicateur", "Libelle" },
                values: new object[] { "2.2", "Recueil des attentes en amont" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "3.1", 3, "Compétences pédagogiques des formateurs", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "3.2", 3, "Dispositifs d'accompagnement" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "4.1", 4, "Moyens techniques et pédagogiques adaptés" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "5.1", 5, "Évaluation à chaud des acquis", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "5.2", 5, "Évaluation à froid des acquis" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "6.1", 6, "Veille métier et compétences" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle" },
                values: new object[] { "6.2", 6, "Veille juridique et administrative" });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "7.1", 7, "Indicateurs de résultats et de satisfaction", 2 });

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CodeIndicateur", "Critere", "Libelle", "NiveauPreuveRequis" },
                values: new object[] { "7.2", 7, "Analyse des indicateurs et actions d'amélioration", 2 });
        }
    }
}
