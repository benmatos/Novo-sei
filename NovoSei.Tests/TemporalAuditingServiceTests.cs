using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Core.Services;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class TemporalAuditingServiceTests
{
    private ApplicationDbContext ObterContextoLocalDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovoSeiDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ObterHistoricoProcessoAsync_DeveRetornarVersoesHistoricas()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var templateEngine = new TemplateEngineService();
        var ldapService = new FakeLdapService();
        var storageService = new FakeFileStorageService();
        var cacheService = new FakeDistributedCache();
        var notificationService = new FakeNotificationService();
        var service = new DocumentoService(db, ldapService, templateEngine, storageService, cacheService, notificationService);

        // Garantir que temos um usuário e um processo de teste
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Login == "admin");
        if (usuario == null)
        {
            usuario = new Usuario { Nome = "Administrador", Login = "admin", Email = "admin@novosei.gov.br", Perfil = "Administrador" };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }

        var numeroUnico = $"SEI-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var processo = new Processo
        {
            NumeroSequencial = numeroUnico,
            Assunto = "Assunto Inicial",
            Status = "Aberto",
            UsuarioId = usuario.Id,
            CriadoEm = DateTime.UtcNow
        };
        db.Processos.Add(processo);
        await db.SaveChangesAsync();

        Console.WriteLine($"=== TEST DEBUG: processo.Id = {processo.Id}, Numero = {processo.NumeroSequencial} ===");

        db.Entry(processo).State = EntityState.Detached;
        await Task.Delay(1000);

        using var dbUpdate = ObterContextoLocalDb();
        var procUpdate = await dbUpdate.Processos.FirstOrDefaultAsync(p => p.NumeroSequencial == numeroUnico);
        if (procUpdate == null)
        {
            Console.WriteLine("=== TEST DEBUG: procUpdate is null when searched by NumeroSequencial ===");
        }
        else
        {
            Console.WriteLine($"=== TEST DEBUG: Found procUpdate with Id = {procUpdate.Id} ===");
            procUpdate.Assunto = "Assunto Alterado";
            dbUpdate.Processos.Update(procUpdate);
            await dbUpdate.SaveChangesAsync();
        }

        // Act
        var historico = await service.ObterHistoricoProcessoAsync(processo.Id);

        // Assert
        Assert.NotNull(historico);
        Assert.True(historico.Count >= 2, $"Esperava-se pelo menos 2 versões do processo no histórico temporal, encontrou {historico.Count}.");
        Assert.Contains(historico, v => v.Assunto == "Assunto Inicial");
        Assert.Contains(historico, v => v.Assunto == "Assunto Alterado");
        Assert.Equal(numeroUnico, historico[0].NumeroSequencial);
    }
}
