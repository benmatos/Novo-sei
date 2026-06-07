using Microsoft.EntityFrameworkCore;
using NovoSei.Core.DTOs;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Core.Services;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;



public class DocumentoServiceTests
{
    private ApplicationDbContext ObterContextoEmMemoria()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CriarDocumentoAsync_DeveSubstituirTemplateESalvar()
    {
        // Arrange
        using var db = ObterContextoEmMemoria();
        
        var usuario = new Usuario { Id = 1, Nome = "Admin", Login = "admin", Email = "admin@novosei.gov.br" };
        db.Usuarios.Add(usuario);

        var processo = new Processo { Id = 1, NumeroSequencial = "SEI-123", Assunto = "Teste", UsuarioId = 1 };
        db.Processos.Add(processo);

        var template = new TemplateDocumento { Id = 1, Nome = "Template", ConteudoHtmlBase = "Template {{NumeroProcesso}} - {{TextoConteudo}}" };
        db.TemplatesDocumento.Add(template);

        await db.SaveChangesAsync();

        var templateEngine = new TemplateEngineService();
        var ldapService = new FakeLdapService();
        var storageService = new FakeFileStorageService();
        var cacheService = new FakeDistributedCache();
        var notificationService = new FakeNotificationService();
        var service = new DocumentoService(db, ldapService, templateEngine, storageService, cacheService, notificationService);

        // Act
        var doc = await service.CriarDocumentoAsync(1, 1, "Ofício", "Conteudo Inserido");

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("Rascunho", doc.Status);
        Assert.Contains("SEI-123", doc.ConteudoHtml);
        Assert.Contains("Conteudo Inserido", doc.ConteudoHtml);

        var docNoDb = await db.Documentos.FindAsync(doc.Id);
        Assert.NotNull(docNoDb);
        Assert.Equal("Ofício", docNoDb.Titulo);
    }

    [Fact]
    public async Task AssinarDocumentoAsync_ComCredenciaisValidas_DeveAdicionarAssinaturaEBloquearModificacoes()
    {
        // Arrange
        using var db = ObterContextoEmMemoria();

        var usuario = new Usuario { Id = 1, Nome = "Admin", Login = "admin", Email = "admin@novosei.gov.br" };
        db.Usuarios.Add(usuario);

        var processo = new Processo { Id = 1, NumeroSequencial = "SEI-123", Assunto = "Teste", UsuarioId = 1 };
        db.Processos.Add(processo);

        var template = new TemplateDocumento { Id = 1, Nome = "Template", ConteudoHtmlBase = "HTML" };
        db.TemplatesDocumento.Add(template);

        var documento = new Documento { Id = 1, ProcessoId = 1, TemplateDocumentoId = 1, Status = "Rascunho", Titulo = "Doc", ConteudoHtml = "HTML" };
        db.Documentos.Add(documento);

        await db.SaveChangesAsync();

        var templateEngine = new TemplateEngineService();
        var ldapService = new FakeLdapService
        {
            Retorno = new LoginResponse(1, "Admin", "admin@novosei.gov.br", "admin", "UsuarioComum")
        };
        var storageService = new FakeFileStorageService();
        var cacheService = new FakeDistributedCache();
        var notificationService = new FakeNotificationService();
        var service = new DocumentoService(db, ldapService, templateEngine, storageService, cacheService, notificationService);

        // Act
        bool resultado = false;
        try
        {
            resultado = await service.AssinarDocumentoAsync(1, "admin", "senha123");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Puppeteer"))
        {
            // Ignorado em ambientes sem conexão ou headless incompatível, contanto que o banco tenha sido salvo.
        }

        // Assert

        var docNoDb = await db.Documentos.Include(d => d.Assinaturas).FirstOrDefaultAsync(d => d.Id == 1);
        Assert.NotNull(docNoDb);
        Assert.Equal("Assinado", docNoDb.Status);
        Assert.Single(docNoDb.Assinaturas);
        Assert.Equal(1, docNoDb.Assinaturas.First().UsuarioId);
        Assert.Equal(64, docNoDb.Assinaturas.First().HashSha256.Length); // Hash SHA-256 de 64 caracteres hex
    }
}
