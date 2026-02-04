using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormationManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "TemplatesDocument",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Stagiaires",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Sessions",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "SessionClients",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "PreuvesQualiopi",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "IndicateursQualiopi",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Formations",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Documents",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Clients",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "ActionsVeille",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 1,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 2,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 3,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 4,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 5,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 6,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 7,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 8,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 9,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 10,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 11,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 12,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 13,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "IndicateursQualiopi",
                keyColumn: "Id",
                keyValue: 14,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "TemplatesDocument",
                keyColumn: "Id",
                keyValue: 1,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "TemplatesDocument",
                keyColumn: "Id",
                keyValue: 2,
                column: "SiteId",
                value: "SITE_01");

            migrationBuilder.UpdateData(
                table: "TemplatesDocument",
                keyColumn: "Id",
                keyValue: 3,
                column: "SiteId",
                value: "SITE_01");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "TemplatesDocument");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Stagiaires");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "SessionClients");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "PreuvesQualiopi");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "IndicateursQualiopi");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "ActionsVeille");
        }
    }
}
