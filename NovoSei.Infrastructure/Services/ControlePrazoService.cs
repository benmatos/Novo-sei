using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class ControlePrazoService(ApplicationDbContext db) : IControlePrazoService
{
    public async Task<ControlePrazo> CriarPrazoAsync(int processoId, int unidadeId, DateTime dataLimite, bool diasUteis, int criadoPorUsuarioId)
    {
        // 1. Cancelar/remover qualquer prazo ativo anterior do mesmo processo na mesma unidade
        var prazosAtivosAnteriores = await db.ControlePrazos
            .Where(c => c.ProcessoId == processoId && c.UnidadeId == unidadeId && c.Status == "Ativo")
            .ToListAsync();

        foreach (var p in prazosAtivosAnteriores)
        {
            p.Status = "Removido";
        }

        // 2. Criar novo controle de prazo
        var novoPrazo = new ControlePrazo
        {
            ProcessoId = processoId,
            UnidadeId = unidadeId,
            DataLimite = dataLimite.Date,
            DiasUteis = diasUteis,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Status = "Ativo"
        };

        db.ControlePrazos.Add(novoPrazo);
        await db.SaveChangesAsync();

        return novoPrazo;
    }

    public async Task<bool> ConcluirPrazoAsync(int prazoId, int resolvidoPorUsuarioId)
    {
        var prazo = await db.ControlePrazos.FindAsync(prazoId);
        if (prazo == null || prazo.Status != "Ativo")
        {
            return false;
        }

        prazo.Status = "Concluido";
        prazo.ResolvidoEm = DateTime.UtcNow;
        prazo.ResolvidoPorUsuarioId = resolvidoPorUsuarioId;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoverPrazoAsync(int prazoId)
    {
        var prazo = await db.ControlePrazos.FindAsync(prazoId);
        if (prazo == null || prazo.Status != "Ativo")
        {
            return false;
        }

        prazo.Status = "Removido";
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ControlePrazo>> ObterPrazosAtivosUnidadeAsync(int unidadeId)
    {
        return await db.ControlePrazos
            .Include(c => c.Processo)
            .Include(c => c.CriadoPorUsuario)
            .Where(c => c.UnidadeId == unidadeId && c.Status == "Ativo")
            .ToListAsync();
    }

    public async Task<ControlePrazo?> ObterPrazoAtivoProcessoUnidadeAsync(int processoId, int unidadeId)
    {
        return await db.ControlePrazos
            .Include(c => c.CriadoPorUsuario)
            .Include(c => c.ResolvidoPorUsuario)
            .FirstOrDefaultAsync(c => c.ProcessoId == processoId && c.UnidadeId == unidadeId && c.Status == "Ativo");
    }
}
