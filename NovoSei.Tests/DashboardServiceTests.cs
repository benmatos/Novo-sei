using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NovoSei.Core.Entities;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NovoSei.Tests;

public class DashboardServiceTests
{
    private ApplicationDbContext ObterContextoEmMemoria()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ObterIndicadoresAsync_DeveRetornarMetricasCorretas()
    {
        // Arrange
        using var db = ObterContextoEmMemoria();

        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Test User",
            Login = "test",
            Email = "test@gov.br"
        };
        db.Usuarios.Add(usuario);

        var template = new TemplateDocumento
        {
            Id = 1,
            Nome = "Template",
            ConteudoHtmlBase = "HTML"
        };
        db.TemplatesDocumento.Add(template);

        // Processos do usuário 1
        var p1 = new Processo { Id = 1, NumeroSequencial = "P1", Status = "Aberto", UsuarioId = 1 };
        var p2 = new Processo { Id = 2, NumeroSequencial = "P2", Status = "Encerrado", UsuarioId = 1 };
        
        // Processo de outro usuário
        var p3 = new Processo { Id = 3, NumeroSequencial = "P3", Status = "Aberto", UsuarioId = 2 };

        db.Processos.AddRange(p1, p2, p3);

        // Documentos do processo do usuário 1
        var d1 = new Documento { Id = 1, ProcessoId = 1, TemplateDocumentoId = 1, Status = "Rascunho", Titulo = "D1", ConteudoHtml = "HTML" };
        var d2 = new Documento { Id = 2, ProcessoId = 1, TemplateDocumentoId = 1, Status = "Assinado", Titulo = "D2", ConteudoHtml = "HTML" };

        db.Documentos.AddRange(d1, d2);

        // Assinatura realizada pelo usuário 1
        var a1 = new Assinatura { Id = 1, DocumentoId = 2, UsuarioId = 1, HashSha256 = "HASH" };
        db.Assinaturas.Add(a1);

        await db.SaveChangesAsync();

        var cache = new FakeDistributedCache();
        var service = new DashboardService(db, cache);

        // Act
        var resultado = await service.ObterIndicadoresAsync(1);

        // Assert
        Assert.Equal(3, resultado.TotalProcessos); // p1, p2, p3
        Assert.Equal(2, resultado.ProcessosAbertos); // p1 e p3
        Assert.Equal(1, resultado.RascunhosPendentes); // apenas d1
        Assert.Equal(1, resultado.TramitesRealizados); // apenas a1
    }
}


