using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovoSei.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoisFatoresHabilitado",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailAlternativo",
                table: "Usuarios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Segredo2Fa",
                table: "Usuarios",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Token2FaAtivacao",
                table: "Usuarios",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenEmailExpiracao",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoisFatoresHabilitado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailAlternativo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Segredo2Fa",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Token2FaAtivacao",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TokenEmailExpiracao",
                table: "Usuarios");
        }
    }
}
