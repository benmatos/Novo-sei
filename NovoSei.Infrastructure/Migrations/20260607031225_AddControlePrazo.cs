using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovoSei.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlePrazo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlePrazos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessoId = table.Column<int>(type: "int", nullable: false),
                    UnidadeId = table.Column<int>(type: "int", nullable: false),
                    DataLimite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiasUteis = table.Column<bool>(type: "bit", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvidoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlePrazos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlePrazos_Processos_ProcessoId",
                        column: x => x.ProcessoId,
                        principalTable: "Processos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlePrazos_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlePrazos_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlePrazos_Usuarios_ResolvidoPorUsuarioId",
                        column: x => x.ResolvidoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlePrazos_CriadoPorUsuarioId",
                table: "ControlePrazos",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlePrazos_ProcessoId",
                table: "ControlePrazos",
                column: "ProcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlePrazos_ResolvidoPorUsuarioId",
                table: "ControlePrazos",
                column: "ResolvidoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlePrazos_UnidadeId",
                table: "ControlePrazos",
                column: "UnidadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlePrazos");
        }
    }
}
