using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class BlocoReuniaoService(ApplicationDbContext db) : IBlocoReuniaoService
{
    public async Task<BlocoReuniao> CriarBlocoAsync(string descricao, int geradoraUnidadeId, int criadoPorUsuarioId)
    {
        var bloco = new BlocoReuniao
        {
            Descricao = descricao,
            GeradoraUnidadeId = geradoraUnidadeId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Status = "Aberto",
            CriadoEm = DateTime.UtcNow
        };

        db.BlocosReuniao.Add(bloco);
        await db.SaveChangesAsync();
        return bloco;
    }

    public async Task<bool> AdicionarProcessosAoBlocoAsync(int blocoId, List<int> processoIds)
    {
        var bloco = await db.BlocosReuniao
            .Include(b => b.Processos)
            .FirstOrDefaultAsync(b => b.Id == blocoId);

        if (bloco == null || bloco.Status != "Aberto")
        {
            return false;
        }

        var processos = await db.Processos
            .Where(p => processoIds.Contains(p.Id))
            .ToListAsync();

        foreach (var proc in processos)
        {
            if (!bloco.Processos.Any(p => p.Id == proc.Id))
            {
                bloco.Processos.Add(proc);
            }
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoverProcessosDoBlocoAsync(int blocoId, List<int> processoIds)
    {
        var bloco = await db.BlocosReuniao
            .Include(b => b.Processos)
            .FirstOrDefaultAsync(b => b.Id == blocoId);

        if (bloco == null || bloco.Status != "Aberto")
        {
            return false;
        }

        var paraRemover = bloco.Processos.Where(p => processoIds.Contains(p.Id)).ToList();
        foreach (var proc in paraRemover)
        {
            bloco.Processos.Remove(proc);
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisponibilizarBlocoAsync(int blocoId, List<int> unidadeReceptoraIds)
    {
        var bloco = await db.BlocosReuniao
            .Include(b => b.BlocoUnidades)
            .FirstOrDefaultAsync(b => b.Id == blocoId);

        if (bloco == null || bloco.Status != "Aberto")
        {
            return false;
        }

        bloco.Status = "Disponibilizado";

        // Limpar compartilhamentos anteriores se houver
        if (bloco.BlocoUnidades.Any())
        {
            db.BlocoReuniaoUnidades.RemoveRange(bloco.BlocoUnidades);
        }

        // Criar registros de permissão/visualização
        foreach (var uniId in unidadeReceptoraIds)
        {
            var share = new BlocoReuniaoUnidade
            {
                BlocoReuniaoId = blocoId,
                UnidadeReceptoraId = uniId,
                Status = "Disponibilizado"
            };
            db.BlocoReuniaoUnidades.Add(share);
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelarDisponibilizacaoAsync(int blocoId)
    {
        var bloco = await db.BlocosReuniao
            .Include(b => b.BlocoUnidades)
            .FirstOrDefaultAsync(b => b.Id == blocoId);

        if (bloco == null || bloco.Status != "Disponibilizado")
        {
            return false;
        }

        bloco.Status = "Aberto";

        // Remover compartilhamentos
        db.BlocoReuniaoUnidades.RemoveRange(bloco.BlocoUnidades);
        
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DevolverBlocoAsync(int blocoId, int unidadeReceptoraId)
    {
        var share = await db.BlocoReuniaoUnidades
            .FirstOrDefaultAsync(bu => bu.BlocoReuniaoId == blocoId && bu.UnidadeReceptoraId == unidadeReceptoraId);

        if (share == null || share.Status != "Disponibilizado")
        {
            return false;
        }

        share.Status = "Devolvido";
        share.DevolvidoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Se todos os destinatários devolveram, atualizar o bloco para "Retornado"
        var bloco = await db.BlocosReuniao
            .Include(b => b.BlocoUnidades)
            .FirstOrDefaultAsync(b => b.Id == blocoId);

        if (bloco != null && bloco.BlocoUnidades.All(bu => bu.Status == "Devolvido"))
        {
            bloco.Status = "Retornado";
            await db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> ConcluirBlocoAsync(int blocoId)
    {
        var bloco = await db.BlocosReuniao.FindAsync(blocoId);
        if (bloco == null || (bloco.Status != "Aberto" && bloco.Status != "Disponibilizado" && bloco.Status != "Retornado"))
        {
            return false;
        }

        bloco.Status = "Concluido";
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<BlocoReuniao>> ObterBlocosGeradosUnidadeAsync(int unidadeId)
    {
        return await db.BlocosReuniao
            .Include(b => b.Processos)
            .Include(b => b.BlocoUnidades)
                .ThenInclude(bu => bu.UnidadeReceptora)
            .Where(b => b.GeradoraUnidadeId == unidadeId)
            .OrderByDescending(b => b.CriadoEm)
            .ToListAsync();
    }

    public async Task<List<BlocoReuniao>> ObterBlocosRecebidosUnidadeAsync(int unidadeId)
    {
        return await db.BlocoReuniaoUnidades
            .Include(bu => bu.BlocoReuniao)
                .ThenInclude(b => b.Processos)
            .Include(bu => bu.BlocoReuniao.CriadoPorUsuario)
            .Where(bu => bu.UnidadeReceptoraId == unidadeId && bu.Status == "Disponibilizado")
            .OrderByDescending(bu => bu.BlocoReuniao.CriadoEm)
            .Select(bu => bu.BlocoReuniao)
            .ToListAsync();
    }

    public async Task<BlocoReuniao?> ObterBlocoComDetalhesAsync(int blocoId)
    {
        return await db.BlocosReuniao
            .Include(b => b.Processos)
                .ThenInclude(p => p.Marcadores)
            .Include(b => b.CriadoPorUsuario)
            .Include(b => b.GeradoraUnidade)
            .Include(b => b.BlocoUnidades)
                .ThenInclude(bu => bu.UnidadeReceptora)
            .FirstOrDefaultAsync(b => b.Id == blocoId);
    }
}
