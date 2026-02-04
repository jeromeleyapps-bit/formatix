using System;
using FormationManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(FormationDbContext))]
    [Migration("20260123120000_AddVeilleRssModule")]
    public partial class AddVeilleRssModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RssFeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DefaultIndicateurId = table.Column<int>(type: "INTEGER", nullable: true),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssFeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RssFeeds_IndicateursQualiopi_DefaultIndicateurId",
                        column: x => x.DefaultIndicateurId,
                        principalTable: "IndicateursQualiopi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RssItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RssFeedId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RssItems_RssFeeds_RssFeedId",
                        column: x => x.RssFeedId,
                        principalTable: "RssFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeilleValidations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RssItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    IndicateurQualiopiId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeilleValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeilleValidations_IndicateursQualiopi_IndicateurQualiopiId",
                        column: x => x.IndicateurQualiopiId,
                        principalTable: "IndicateursQualiopi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VeilleValidations_RssItems_RssItemId",
                        column: x => x.RssItemId,
                        principalTable: "RssItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RssFeeds_DefaultIndicateurId",
                table: "RssFeeds",
                column: "DefaultIndicateurId");

            migrationBuilder.CreateIndex(
                name: "IX_RssFeeds_IsActive",
                table: "RssFeeds",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RssFeeds_Url",
                table: "RssFeeds",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_RssItems_FetchedAt",
                table: "RssItems",
                column: "FetchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RssItems_PublishedUtc",
                table: "RssItems",
                column: "PublishedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RssItems_RssFeedId_ExternalId",
                table: "RssItems",
                columns: new[] { "RssFeedId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeilleValidations_IndicateurQualiopiId",
                table: "VeilleValidations",
                column: "IndicateurQualiopiId");

            migrationBuilder.CreateIndex(
                name: "IX_VeilleValidations_SiteId",
                table: "VeilleValidations",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_VeilleValidations_ValidatedAt",
                table: "VeilleValidations",
                column: "ValidatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VeilleValidations");
            migrationBuilder.DropTable(name: "RssItems");
            migrationBuilder.DropTable(name: "RssFeeds");
        }
    }
}
