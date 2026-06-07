using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class MarcadoresEComentariosTests
{
    private ApplicationDbContext ObterContextoLocalDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovoSeiDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<Usuario> ObterOuCriarUsuarioAdminAsync(ApplicationDbContext db)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Login == "admin");
        if (usuario == null)
        {
            usuario = new Usuario { Nome = "Administrador", Login = "admin", Email = "admin@novosei.gov.br", Perfil = "Administrador" };
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

        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Sigla == "TESTE");
        if (unidade == null)
        {
            unidade = new Unidade { Sigla = "TESTE", Descricao = "Unidade de Teste", OrgaoId = orgao.Id };
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
            Assunto = "Processo Teste Marcadores",
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
    public async Task MarcadorService_FluxoCompleto_DeveFuncionarCorretamente()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new MarcadorService(db);
        
        var usuario = await ObterOuCriarUsuarioAdminAsync(db);
        var unidade = await ObterOuCriarUnidadeTesteAsync(db);
        var processo = await CriarProcessoTesteAsync(db, usuario, unidade);

        string nomeMarcador = $"Marcador-{Guid.NewGuid().ToString()[..6]}";
        string corHex = "#EF4444";

        // Act & Assert: Criar Marcador
        var marcador = await service.CriarMarcadorAsync(unidade.Id, nomeMarcador, corHex);
        Assert.NotNull(marcador);
        Assert.Equal(nomeMarcador, marcador.Nome);
        Assert.Equal(corHex, marcador.CorHex);
        Assert.True(marcador.Ativo);

        // Act & Assert: Associar ao Processo
        var associou = await service.AssociarMarcadorAoProcessoAsync(processo.Id, marcador.Id);
        Assert.True(associou);

        // Verificar se associou no banco
        var procComMarcador = await db.Processos
            .Include(p => p.Marcadores)
            .FirstOrDefaultAsync(p => p.Id == processo.Id);
        Assert.NotNull(procComMarcador);
        Assert.Contains(procComMarcador.Marcadores, m => m.Id == marcador.Id);

        // Act & Assert: Tentar excluir marcador associado a processo deve lançar exceção
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExcluirMarcadorAsync(marcador.Id));

        // Act & Assert: Desassociar do Processo
        var desassociou = await service.DesassociarMarcadorDoProcessoAsync(processo.Id, marcador.Id);
        Assert.True(desassociou);

        // Act & Assert: Agora que não está associado, a exclusão deve funcionar
        var excluiu = await service.ExcluirMarcadorAsync(marcador.Id);
        Assert.True(excluiu);

        var marcadorDeletado = await db.Marcadores.FindAsync(marcador.Id);
        Assert.Null(marcadorDeletado);
    }

    [Fact]
    public async Task ComentarioService_FluxoCompleto_DeveFuncionarCorretamente()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new ComentarioService(db);

        var usuario = await ObterOuCriarUsuarioAdminAsync(db);
        var unidade = await ObterOuCriarUnidadeTesteAsync(db);
        var processo = await CriarProcessoTesteAsync(db, usuario, unidade);

        string textoComentario = "Este é um comentário de teste para auditoria.";

        // Act: Criar Comentário
        var comentario = await service.CriarComentarioProcessoAsync(processo.Id, textoComentario, usuario.Id, unidade.Id);

        // Assert
        Assert.NotNull(comentario);
        Assert.Equal(textoComentario, comentario.Descricao);
        Assert.Equal(unidade.Id, comentario.UnidadeId);
        Assert.Equal(usuario.Id, comentario.UsuarioId);

        // Act: Listar Comentários
        var lista = await service.ObterComentariosDoProcessoAsync(processo.Id, usuario.Id, unidade.Id);
        Assert.NotEmpty(lista);
        Assert.Contains(lista, c => c.Id == comentario.Id);

        // Act: Alterar Comentário
        string novoTexto = "Comentário alterado para fins de teste.";
        var alterou = await service.AlterarComentarioAsync(comentario.Id, novoTexto, unidade.Id);
        Assert.True(alterou);

        var comentarioModificado = await db.Comentarios.FindAsync(comentario.Id);
        Assert.NotNull(comentarioModificado);
        Assert.Equal(novoTexto, comentarioModificado.Descricao);

        // Act & Assert: Tentar excluir comentário de outra unidade deve lançar exceção
        int outraUnidadeId = 9999;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExcluirComentarioAsync(comentario.Id, outraUnidadeId));

        // Act: Excluir Comentário da mesma unidade
        var excluiu = await service.ExcluirComentarioAsync(comentario.Id, unidade.Id);
        Assert.True(excluiu);

        var comentarioDeletado = await db.Comentarios.FindAsync(comentario.Id);
        Assert.Null(comentarioDeletado);
    }
}
