using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class BlocoReuniaoTests
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
            usuario = new Usuario { Nome = "Usuário Teste Blocos", Login = login, Email = $"{login}@novosei.gov.br", Perfil = "UsuarioComum" };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }
        return usuario;
    }

    private async Task<Unidade> ObterOuCriarUnidadeTesteAsync(ApplicationDbContext db, string sigla)
    {
        var orgao = await db.Orgaos.FirstOrDefaultAsync();
        if (orgao == null)
        {
            orgao = new Orgao { Sigla = "TST", Descricao = "Órgão Teste" };
            db.Orgaos.Add(orgao);
            await db.SaveChangesAsync();
        }

        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Sigla == sigla);
        if (unidade == null)
        {
            unidade = new Unidade { Sigla = sigla, Descricao = $"Unidade {sigla}", OrgaoId = orgao.Id };
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
            Assunto = "Processo Teste Blocos",
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
    public async Task BlocoReuniaoService_FluxoCompletoCompartilhamento_DeveFuncionarCorretamente()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new BlocoReuniaoService(db);

        var usuario = await ObterOuCriarUsuarioTesteAsync(db, "user_blocos");
        var unidadeGeradora = await ObterOuCriarUnidadeTesteAsync(db, "UNI_GERADORA");
        var unidadeReceptora = await ObterOuCriarUnidadeTesteAsync(db, "UNI_RECEPTORA");
        var processo = await CriarProcessoTesteAsync(db, usuario, unidadeGeradora);

        // Act & Assert 1: Criar Bloco
        var bloco = await service.CriarBlocoAsync("Reunião Semanal de Projetos", unidadeGeradora.Id, usuario.Id);
        Assert.NotNull(bloco);
        Assert.Equal("Aberto", bloco.Status);
        Assert.Equal("Reunião Semanal de Projetos", bloco.Descricao);

        // Act & Assert 2: Adicionar Processo ao Bloco
        var adicionou = await service.AdicionarProcessosAoBlocoAsync(bloco.Id, [processo.Id]);
        Assert.True(adicionou);

        // Obter detalhes e verificar processo
        var blocoDetalhe = await service.ObterBlocoComDetalhesAsync(bloco.Id);
        Assert.NotNull(blocoDetalhe);
        Assert.Contains(blocoDetalhe.Processos, p => p.Id == processo.Id);

        // Act & Assert 3: Disponibilizar Bloco para Unidade Receptora
        var disponibilizou = await service.DisponibilizarBlocoAsync(bloco.Id, [unidadeReceptora.Id]);
        Assert.True(disponibilizou);

        // Verificar status do bloco
        db.ChangeTracker.Clear();
        var blocoDisp = await db.BlocosReuniao.FindAsync(bloco.Id);
        Assert.NotNull(blocoDisp);
        Assert.Equal("Disponibilizado", blocoDisp.Status);

        // Verificar recebimento pela unidade receptora
        var recebidos = await service.ObterBlocosRecebidosUnidadeAsync(unidadeReceptora.Id);
        Assert.Contains(recebidos, b => b.Id == bloco.Id);

        // Act & Assert 4: Devolver Bloco (Unidade Receptora)
        var devolveu = await service.DevolverBlocoAsync(bloco.Id, unidadeReceptora.Id);
        Assert.True(devolveu);

        // Como foi a única receptora e devolveu, status do bloco deve ir para "Retornado"
        db.ChangeTracker.Clear();
        var blocoRetornado = await db.BlocosReuniao.FindAsync(bloco.Id);
        Assert.NotNull(blocoRetornado);
        Assert.Equal("Retornado", blocoRetornado.Status);

        // Act & Assert 5: Concluir Bloco
        var concluiu = await service.ConcluirBlocoAsync(bloco.Id);
        Assert.True(concluiu);

        var blocoConcluido = await db.BlocosReuniao.FindAsync(bloco.Id);
        Assert.NotNull(blocoConcluido);
        Assert.Equal("Concluido", blocoConcluido.Status);

        // Limpeza dos dados de teste
        db.BlocoReuniaoUnidades.RemoveRange(db.BlocoReuniaoUnidades.Where(bu => bu.BlocoReuniaoId == bloco.Id));
        db.BlocosReuniao.Remove(blocoConcluido);
        db.Processos.Remove(processo);
        await db.SaveChangesAsync();
    }
}
