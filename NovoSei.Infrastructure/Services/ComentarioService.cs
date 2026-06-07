using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class ComentarioService(ApplicationDbContext db) : IComentarioService
{
    public async Task<List<Comentario>> ObterComentariosDoProcessoAsync(int processoId, int usuarioId, int unidadeId)
    {
        var comments = await db.Comentarios
            .Include(c => c.Usuario)
            .Include(c => c.Documento)
            .Where(c => c.ProcessoId == processoId)
            .ToListAsync();

        // Regra do Manual: Comentários em documentos internos não assinados (rascunhos)
        // só serão visualizados no âmbito da unidade que o inseriu.
        return comments.Where(c => 
            c.DocumentoId == null || 
            c.Documento == null || 
            c.Documento.Status != "Rascunho" || 
            c.UnidadeId == unidadeId
        )
        .OrderBy(c => c.CriadoEm)
        .ToList();
    }

    public async Task<List<Comentario>> ObterComentariosDoDocumentoAsync(int documentoId, int usuarioId, int unidadeId)
    {
        var doc = await db.Documentos.FindAsync(documentoId);
        if (doc == null) return [];

        var comments = await db.Comentarios
            .Include(c => c.Usuario)
            .Where(c => c.DocumentoId == documentoId)
            .ToListAsync();

        // Se for rascunho, só exibe comentários feitos pela própria unidade
        if (doc.Status == "Rascunho")
        {
            return comments.Where(c => c.UnidadeId == unidadeId)
                .OrderBy(c => c.CriadoEm)
                .ToList();
        }

        return comments.OrderBy(c => c.CriadoEm).ToList();
    }

    public async Task<Comentario> CriarComentarioProcessoAsync(int processoId, string descricao, int usuarioId, int unidadeId)
    {
        var processo = await db.Processos.FindAsync(processoId)
            ?? throw new ArgumentException("Processo não encontrado.");

        var comentario = new Comentario
        {
            ProcessoId = processoId,
            Descricao = descricao,
            UsuarioId = usuarioId,
            UnidadeId = unidadeId,
            CriadoEm = DateTime.UtcNow
        };

        db.Comentarios.Add(comentario);
        await db.SaveChangesAsync();
        return comentario;
    }

    public async Task<Comentario> CriarComentarioDocumentoAsync(int documentoId, string descricao, int usuarioId, int unidadeId)
    {
        var doc = await db.Documentos.FindAsync(documentoId)
            ?? throw new ArgumentException("Documento não encontrado.");

        var comentario = new Comentario
        {
            ProcessoId = doc.ProcessoId,
            DocumentoId = documentoId,
            Descricao = descricao,
            UsuarioId = usuarioId,
            UnidadeId = unidadeId,
            CriadoEm = DateTime.UtcNow
        };

        db.Comentarios.Add(comentario);
        await db.SaveChangesAsync();
        return comentario;
    }

    public async Task<bool> ExcluirComentarioAsync(int comentarioId, int unidadeId)
    {
        var comentario = await db.Comentarios.FindAsync(comentarioId);
        if (comentario == null) return false;

        // Regra do Manual: Só podem ser alterados ou excluídos pela unidade que os inseriu.
        if (comentario.UnidadeId != unidadeId)
        {
            throw new UnauthorizedAccessException("Este comentário só pode ser excluído pela unidade que o criou.");
        }

        db.Comentarios.Remove(comentario);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AlterarComentarioAsync(int comentarioId, string descricao, int unidadeId)
    {
        var comentario = await db.Comentarios.FindAsync(comentarioId);
        if (comentario == null) return false;

        // Regra do Manual: Só podem ser alterados ou excluídos pela unidade que os inseriu.
        if (comentario.UnidadeId != unidadeId)
        {
            throw new UnauthorizedAccessException("Este comentário só pode ser alterado pela unidade que o criou.");
        }

        comentario.Descricao = descricao;
        await db.SaveChangesAsync();
        return true;
    }
}
