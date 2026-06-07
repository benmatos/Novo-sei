using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class ControlePrazoTests
{
    private ApplicationDbContext ObterContextoLocalDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovoSeiDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<Usuario> ObterOuCriarUsuarioTesteAsync(ApplicationDbContext db, string login)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Login == login);
        if (usuario == null)
        {
            usuario = new Usuario { Nome = "Usuário Teste Prazos", Login = login, Email = $"{login}@novosei.gov.br", Perfil = "UsuarioComum" };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }
        return usuario;
    }

    private async Task<Unidade> ObterOuCriarUnidadeTesteAsync(ApplicationDbContext db)
    {
        var orgao = await db.Orgaos.FirstOrDefaultAsync();
        if (orgao == null)
        {
            orgao = new Orgao { Sigla = "TST", Descricao = "Órgão Teste" };
            db.Orgaos.Add(orgao);
            await db.SaveChangesAsync();
        }

        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Sigla == "TESTE_PRAZOS");
        if (unidade == null)
        {
            unidade = new Unidade { Sigla = "TESTE_PRAZOS", Descricao = "Unidade Teste Prazos", OrgaoId = orgao.Id };
            db.Unidades.Add(unidade);
            await db.SaveChangesAsync();
        }
        return unidade;
    }

    private async Task<Processo> CriarProcessoTesteAsync(ApplicationDbContext db, Usuario usuario, Unidade unidade)
    {
        var numeroUnico = $"SEI-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var processo = new Processo
        {
            NumeroSequencial = numeroUnico,
            Assunto = "Processo Teste Prazos",
            Status = "Aberto",
            UsuarioId = usuario.Id,
            UnidadeId = unidade.Id,
            CriadoEm = DateTime.UtcNow
        };
        db.Processos.Add(processo);
        await db.SaveChangesAsync();
        return processo;
    }

    [Fact]
    public async Task ControlePrazoService_FluxoCompleto_DeveFuncionarCorretamente()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new ControlePrazoService(db);

        var usuario = await ObterOuCriarUsuarioTesteAsync(db, "user_prazos");
        var unidade = await ObterOuCriarUnidadeTesteAsync(db);
        var processo = await CriarProcessoTesteAsync(db, usuario, unidade);

        var dataLimite = DateTime.Today.AddDays(7);

        // Act & Assert 1: Criar Prazo
        var prazo = await service.CriarPrazoAsync(processo.Id, unidade.Id, dataLimite, true, usuario.Id);
        Assert.NotNull(prazo);
        Assert.Equal("Ativo", prazo.Status);
        Assert.Equal(dataLimite.Date, prazo.DataLimite);
        Assert.True(prazo.DiasUteis);
        Assert.Equal(usuario.Id, prazo.CriadoPorUsuarioId);

        // Verificar se aparece na listagem de ativos da unidade
        var ativos = await service.ObterPrazosAtivosUnidadeAsync(unidade.Id);
        Assert.Contains(ativos, p => p.Id == prazo.Id);

        // Act & Assert 2: Criar novo prazo para o mesmo processo/unidade deve remover o anterior
        var novaDataLimite = DateTime.Today.AddDays(10);
        var novoPrazo = await service.CriarPrazoAsync(processo.Id, unidade.Id, novaDataLimite, false, usuario.Id);
        
        // Obter prazo antigo no banco
        db.ChangeTracker.Clear();
        var antigoNoDb = await db.ControlePrazos.FindAsync(prazo.Id);
        Assert.NotNull(antigoNoDb);
        Assert.Equal("Removido", antigoNoDb.Status);

        Assert.Equal("Ativo", novoPrazo.Status);

        // Act & Assert 3: Concluir Prazo
        var concluido = await service.ConcluirPrazoAsync(novoPrazo.Id, usuario.Id);
        Assert.True(concluido);

        var concluidoNoDb = await db.ControlePrazos.FindAsync(novoPrazo.Id);
        Assert.NotNull(concluidoNoDb);
        Assert.Equal("Concluido", concluidoNoDb.Status);
        Assert.NotNull(concluidoNoDb.ResolvidoEm);
        Assert.Equal(usuario.Id, concluidoNoDb.ResolvidoPorUsuarioId);

        // Limpeza do banco de dados para evitar acúmulo
        db.ControlePrazos.RemoveRange(db.ControlePrazos.Where(c => c.ProcessoId == processo.Id));
        db.Processos.Remove(processo);
        await db.SaveChangesAsync();
    }
}
