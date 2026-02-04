using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class QualiopiIndicatorsSiteUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IndicateursQualiopi_CodeIndicateur",
                table: "IndicateursQualiopi");

            migrationBuilder.CreateIndex(
                name: "IX_IndicateursQualiopi_SiteId_CodeIndicateur",
                table: "IndicateursQualiopi",
                columns: new[] { "SiteId", "CodeIndicateur" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IndicateursQualiopi_SiteId_CodeIndicateur",
                table: "IndicateursQualiopi");

            migrationBuilder.CreateIndex(
                name: "IX_IndicateursQualiopi_CodeIndicateur",
                table: "IndicateursQualiopi",
                column: "CodeIndicateur",
                unique: true);
        }
    }
}
