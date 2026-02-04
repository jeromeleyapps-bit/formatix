using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Biographie = table.Column<string>(type: "TEXT", nullable: false),
                    Competences = table.Column<string>(type: "TEXT", nullable: false),
                    Experience = table.Column<string>(type: "TEXT", nullable: false),
                    Formations = table.Column<string>(type: "TEXT", nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeClient = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", nullable: false),
                    CodePostal = table.Column<string>(type: "TEXT", nullable: false),
                    Ville = table.Column<string>(type: "TEXT", nullable: false),
                    SIRET = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroOPCA = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DureeHeures = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PrixIndicatif = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EstPublique = table.Column<bool>(type: "INTEGER", nullable: false),
                    Programme = table.Column<string>(type: "TEXT", nullable: false),
                    Prerequis = table.Column<string>(type: "TEXT", nullable: false),
                    ModalitesPedagogiques = table.Column<string>(type: "TEXT", nullable: false),
                    ModalitesEvaluation = table.Column<string>(type: "TEXT", nullable: false),
                    ReferencesQualiopi = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndicateursQualiopi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodeIndicateur = table.Column<string>(type: "TEXT", nullable: false),
                    Libelle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Critere = table.Column<int>(type: "INTEGER", nullable: false),
                    NiveauPreuveRequis = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicateursQualiopi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplatesDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeDocument = table.Column<int>(type: "INTEGER", nullable: false),
                    Contenu = table.Column<string>(type: "TEXT", nullable: false),
                    Styles = table.Column<string>(type: "TEXT", nullable: false),
                    Variables = table.Column<string>(type: "TEXT", nullable: false),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatesDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionsVeille",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormateurId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DateAction = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duree = table.Column<decimal>(type: "TEXT", nullable: false),
                    Preuves = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionsVeille", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionsVeille_AspNetUsers_FormateurId",
                        column: x => x.FormateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Lieu = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EstPublique = table.Column<bool>(type: "INTEGER", nullable: false),
                    Statut = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Programmée"),
                    NombreMaxStagiaires = table.Column<int>(type: "INTEGER", nullable: false),
                    FormateurId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_AspNetUsers_FormateurId",
                        column: x => x.FormateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreuvesQualiopi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IndicateurQualiopiId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CheminFichier = table.Column<string>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: false),
                    EstValide = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CommentaireValidation = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreuvesQualiopi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreuvesQualiopi_IndicateursQualiopi_IndicateurQualiopiId",
                        column: x => x.IndicateurQualiopiId,
                        principalTable: "IndicateursQualiopi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreuvesQualiopi_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    TarifNegocie = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NombrePlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeFinancement = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionClients_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionClients_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stagiaires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DateNaissance = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Poste = table.Column<string>(type: "TEXT", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    StatutInscription = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HeuresPresence = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    EvaluationAChaud = table.Column<decimal>(type: "TEXT", nullable: true),
                    EvaluationAFroid = table.Column<decimal>(type: "TEXT", nullable: true),
                    CommentairesEvaluation = table.Column<string>(type: "TEXT", nullable: false),
                    AttestationGeneree = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAttestation = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stagiaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stagiaires_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Stagiaires_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeDocument = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Donnees = table.Column<string>(type: "TEXT", nullable: false),
                    StatutValidation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ValideurId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheminFichier = table.Column<string>(type: "TEXT", nullable: false),
                    NomFichier = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: true),
                    StagiaireId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreePar = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiePar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ValideurId",
                        column: x => x.ValideurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Stagiaires_StagiaireId",
                        column: x => x.StagiaireId,
                        principalTable: "Stagiaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_TemplatesDocument_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TemplatesDocument",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "IndicateursQualiopi",
                columns: new[] { "Id", "CodeIndicateur", "CreePar", "Critere", "DateCreation", "DateModification", "Description", "Libelle", "ModifiePar", "NiveauPreuveRequis" },
                values: new object[,]
                {
                    { 1, "1.1", "system", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Information préalable sur les objectifs, durée, contenu", "system", 2 },
                    { 2, "1.2", "system", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Modalités d'inscription et délais", "system", 1 },
                    { 3, "1.3", "system", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Tarifs et modalités de paiement", "system", 2 },
                    { 4, "2.1", "system", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Adaptation aux besoins du public", "system", 2 },
                    { 5, "2.2", "system", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Recueil des attentes en amont", "system", 1 },
                    { 6, "3.1", "system", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Compétences pédagogiques des formateurs", "system", 2 },
                    { 7, "3.2", "system", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Dispositifs d'accompagnement", "system", 1 },
                    { 8, "4.1", "system", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Moyens techniques et pédagogiques adaptés", "system", 1 },
                    { 9, "5.1", "system", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Évaluation à chaud des acquis", "system", 2 },
                    { 10, "5.2", "system", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Évaluation à froid des acquis", "system", 1 },
                    { 11, "6.1", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Veille métier et compétences", "system", 1 },
                    { 12, "6.2", "system", 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Veille juridique et administrative", "system", 1 },
                    { 13, "7.1", "system", 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Indicateurs de résultats et de satisfaction", "system", 2 },
                    { 14, "7.2", "system", 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Analyse des indicateurs et actions d'amélioration", "system", 2 }
                });

            migrationBuilder.InsertData(
                table: "TemplatesDocument",
                columns: new[] { "Id", "Actif", "Contenu", "CreePar", "DateCreation", "DateModification", "ModifiePar", "Nom", "Styles", "TypeDocument", "Variables" },
                values: new object[,]
                {
                    { 1, true, "<html><body><h1>Convention de formation</h1>{{formation.titre}}</body></html>", "system", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "system", "Convention de formation", "", 0, "{\"formation.titre\":\"Titre de la formation\"}" },
                    { 2, true, "<html><body><h1>Attestation</h1>{{stagiaire.nom}} {{stagiaire.prenom}}</body></html>", "system", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "system", "Attestation de fin de formation", "", 2, "{\"stagiaire.nom\":\"Nom du stagiaire\",\"stagiaire.prenom\":\"Prénom du stagiaire\"}" },
                    { 3, true, "<html><body><h1>Feuille d'émargement</h1>{{session.titre}}</body></html>", "system", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "system", "Émargement", "", 3, "{\"session.titre\":\"Titre de la session\"}" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionsVeille_DateAction",
                table: "ActionsVeille",
                column: "DateAction");

            migrationBuilder.CreateIndex(
                name: "IX_ActionsVeille_FormateurId",
                table: "ActionsVeille",
                column: "FormateurId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionsVeille_Type",
                table: "ActionsVeille",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Role",
                table: "AspNetUsers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Nom",
                table: "Clients",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_SIRET",
                table: "Clients",
                column: "SIRET",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClientId",
                table: "Documents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SessionId",
                table: "Documents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StagiaireId",
                table: "Documents",
                column: "StagiaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StatutValidation",
                table: "Documents",
                column: "StatutValidation");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TemplateId",
                table: "Documents",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TypeDocument",
                table: "Documents",
                column: "TypeDocument");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ValideurId",
                table: "Documents",
                column: "ValideurId");

            migrationBuilder.CreateIndex(
                name: "IX_Formations_Titre",
                table: "Formations",
                column: "Titre");

            migrationBuilder.CreateIndex(
                name: "IX_IndicateursQualiopi_CodeIndicateur",
                table: "IndicateursQualiopi",
                column: "CodeIndicateur",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndicateursQualiopi_Critere",
                table: "IndicateursQualiopi",
                column: "Critere");

            migrationBuilder.CreateIndex(
                name: "IX_PreuvesQualiopi_EstValide",
                table: "PreuvesQualiopi",
                column: "EstValide");

            migrationBuilder.CreateIndex(
                name: "IX_PreuvesQualiopi_IndicateurQualiopiId_SessionId",
                table: "PreuvesQualiopi",
                columns: new[] { "IndicateurQualiopiId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PreuvesQualiopi_SessionId",
                table: "PreuvesQualiopi",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionClients_ClientId",
                table: "SessionClients",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionClients_SessionId_ClientId",
                table: "SessionClients",
                columns: new[] { "SessionId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_DateDebut",
                table: "Sessions",
                column: "DateDebut");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FormateurId",
                table: "Sessions",
                column: "FormateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FormationId",
                table: "Sessions",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Statut",
                table: "Sessions",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Stagiaires_ClientId_Nom_Prenom",
                table: "Stagiaires",
                columns: new[] { "ClientId", "Nom", "Prenom" });

            migrationBuilder.CreateIndex(
                name: "IX_Stagiaires_SessionId",
                table: "Stagiaires",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatesDocument_TypeDocument",
                table: "TemplatesDocument",
                column: "TypeDocument");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionsVeille");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "PreuvesQualiopi");

            migrationBuilder.DropTable(
                name: "SessionClients");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Stagiaires");

            migrationBuilder.DropTable(
                name: "TemplatesDocument");

            migrationBuilder.DropTable(
                name: "IndicateursQualiopi");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Formations");
        }
    }
}
