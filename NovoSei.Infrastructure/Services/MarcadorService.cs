using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class MarcadorService(ApplicationDbContext db) : IMarcadorService
{
    public async Task<List<Marcador>> ObterMarcadoresDaUnidadeAsync(int unidadeId)
    {
        return await db.Marcadores
            .Where(m => m.UnidadeId == unidadeId)
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<Marcador> CriarMarcadorAsync(int unidadeId, string nome, string corHex)
    {
        var marcador = new Marcador
        {
            UnidadeId = unidadeId,
            Nome = nome,
            CorHex = corHex,
            Ativo = true
        };

        db.Marcadores.Add(marcador);
        await db.SaveChangesAsync();
        return marcador;
    }

    public async Task<Marcador?> ObterMarcadorPorIdAsync(int id)
    {
        return await db.Marcadores.FindAsync(id);
    }

    public async Task<bool> AtualizarMarcadorAsync(int id, string nome, string corHex)
    {
        var marcador = await db.Marcadores.FindAsync(id);
        if (marcador == null) return false;

        marcador.Nome = nome;
        marcador.CorHex = corHex;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AlternarAtivoMarcadorAsync(int id, bool ativo)
    {
        var marcador = await db.Marcadores.FindAsync(id);
        if (marcador == null) return false;

        marcador.Ativo = ativo;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExcluirMarcadorAsync(int id)
    {
        var marcador = await db.Marcadores
            .Include(m => m.Processos)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (marcador == null) return false;

        // Se o marcador já foi associado a algum processo, ele não pode ser excluído, apenas desativado (Regra SEI 4.0)
        if (marcador.Processos.Any())
        {
            throw new InvalidOperationException("Não é possível excluir um marcador que já foi associado a processos. Por favor, desative-o em vez disso.");
        }

        db.Marcadores.Remove(marcador);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssociarMarcadorAoProcessoAsync(int processoId, int marcadorId)
    {
        var processo = await db.Processos
            .Include(p => p.Marcadores)
            .FirstOrDefaultAsync(p => p.Id == processoId);

        var marcador = await db.Marcadores.FindAsync(marcadorId);
        
        if (processo == null || marcador == null) return false;
        if (!marcador.Ativo) throw new InvalidOperationException("Não é possível associar um marcador inativo.");

        // Se já estiver associado, não duplica
        if (processo.Marcadores.Any(m => m.Id == marcadorId)) return true;

        processo.Marcadores.Add(marcador);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesassociarMarcadorDoProcessoAsync(int processoId, int marcadorId)
    {
        var processo = await db.Processos
            .Include(p => p.Marcadores)
            .FirstOrDefaultAsync(p => p.Id == processoId);

        if (processo == null) return false;

        var marcador = processo.Marcadores.FirstOrDefault(m => m.Id == marcadorId);
        if (marcador == null) return true;

        processo.Marcadores.Remove(marcador);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssociarMarcadoresAoProcessoEmLoteAsync(List<int> processosIds, int marcadorId)
    {
        var marcador = await db.Marcadores.FindAsync(marcadorId);
        if (marcador == null) return false;
        if (!marcador.Ativo) throw new InvalidOperationException("Não é possível associar um marcador inativo.");

        var processos = await db.Processos
            .Include(p => p.Marcadores)
            .Where(p => processosIds.Contains(p.Id))
            .ToListAsync();

        foreach (var proc in processos)
        {
            if (!proc.Marcadores.Any(m => m.Id == marcadorId))
            {
                proc.Marcadores.Add(marcador);
            }
        }

        await db.SaveChangesAsync();
        return true;
    }
}
