using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NovoSei.Core.DTOs;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace NovoSei.Infrastructure.Services;

public class DashboardService(ApplicationDbContext db, IDistributedCache cache) : IDashboardService
{
    public async Task<DashboardDto> ObterIndicadoresAsync(int usuarioId)
    {
        var cacheKey = $"dashboard:usuario:{usuarioId}";
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var dto = JsonSerializer.Deserialize<DashboardDto>(cachedData);
                if (dto != null)
                    return dto;
            }
        }
        catch
        {
            // Fallback para banco se o cache/Redis falhar
        }

        var totalProcessos = await db.Processos
            .CountAsync();

        var processosAbertos = await db.Processos
            .CountAsync(p => p.Status == "Aberto");

        var rascunhosPendentes = await db.Documentos
            .CountAsync(d => d.Status == "Rascunho");

        var tramitesRealizados = await db.Assinaturas
            .CountAsync();

        var novoDto = new DashboardDto(
            totalProcessos,
            processosAbertos,
            rascunhosPendentes,
            tramitesRealizados
        );

        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(novoDto), cacheOptions);
        }
        catch
        {
            // Ignorado se falhar gravação em cache
        }

        return novoDto;
    }

    public async Task InvalidarCacheAsync(int usuarioId)
    {
        var cacheKey = $"dashboard:usuario:{usuarioId}";
        try
        {
            await cache.RemoveAsync(cacheKey);
        }
        catch
        {
            // Ignorado se falhar invalidação em cache
        }
    }
}
