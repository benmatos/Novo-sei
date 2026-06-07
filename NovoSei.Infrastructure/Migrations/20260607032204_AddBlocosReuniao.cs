using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovoSei.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlocosReuniao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlocosReuniao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeradoraUnidadeId = table.Column<int>(type: "int", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocosReuniao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlocosReuniao_Unidades_GeradoraUnidadeId",
                        column: x => x.GeradoraUnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlocosReuniao_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlocoReuniaoUnidades",
                columns: table => new
                {
                    BlocoReuniaoId = table.Column<int>(type: "int", nullable: false),
                    UnidadeReceptoraId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DevolvidoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocoReuniaoUnidades", x => new { x.BlocoReuniaoId, x.UnidadeReceptoraId });
                    table.ForeignKey(
                        name: "FK_BlocoReuniaoUnidades_BlocosReuniao_BlocoReuniaoId",
                        column: x => x.BlocoReuniaoId,
                        principalTable: "BlocosReuniao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlocoReuniaoUnidades_Unidades_UnidadeReceptoraId",
                        column: x => x.UnidadeReceptoraId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlocosReuniaoProcessos",
                columns: table => new
                {
                    BlocoReuniaoId = table.Column<int>(type: "int", nullable: false),
                    ProcessosId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocosReuniaoProcessos", x => new { x.BlocoReuniaoId, x.ProcessosId });
                    table.ForeignKey(
                        name: "FK_BlocosReuniaoProcessos_BlocosReuniao_BlocoReuniaoId",
                        column: x => x.BlocoReuniaoId,
                        principalTable: "BlocosReuniao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlocosReuniaoProcessos_Processos_ProcessosId",
                        column: x => x.ProcessosId,
                        principalTable: "Processos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlocoReuniaoUnidades_UnidadeReceptoraId",
                table: "BlocoReuniaoUnidades",
                column: "UnidadeReceptoraId");

            migrationBuilder.CreateIndex(
                name: "IX_BlocosReuniao_CriadoPorUsuarioId",
                table: "BlocosReuniao",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_BlocosReuniao_GeradoraUnidadeId",
                table: "BlocosReuniao",
                column: "GeradoraUnidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_BlocosReuniaoProcessos_ProcessosId",
                table: "BlocosReuniaoProcessos",
                column: "ProcessosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlocoReuniaoUnidades");

            migrationBuilder.DropTable(
                name: "BlocosReuniaoProcessos");

            migrationBuilder.DropTable(
                name: "BlocosReuniao");
        }
    }
}
