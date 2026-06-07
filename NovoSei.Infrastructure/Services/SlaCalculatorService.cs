using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class SlaCalculatorService(ApplicationDbContext db) : ISlaCalculatorService
{
    public async Task<int> CalcularDiasRestantesAsync(DateTime dataLimite, bool contarDiasUteis)
    {
        var hoje = DateTime.Today;
        var limite = dataLimite.Date;

        if (hoje == limite)
        {
            return 0;
        }

        if (!contarDiasUteis)
        {
            return (limite - hoje).Days;
        }

        bool eOverdue = limite < hoje;
        var dataInicio = eOverdue ? limite : hoje;
        var dataFim = eOverdue ? hoje : limite;

        var feriados = await db.Feriados
            .Select(f => f.Data.Date)
            .ToListAsync();
        var hashFeriados = new HashSet<DateTime>(feriados);

        int diasUteis = 0;
        var dataCorrente = dataInicio.AddDays(1);

        while (dataCorrente <= dataFim)
        {
            bool eFimDeSemana = dataCorrente.DayOfWeek == DayOfWeek.Saturday || dataCorrente.DayOfWeek == DayOfWeek.Sunday;
            bool eFeriado = hashFeriados.Contains(dataCorrente);

            if (!eFimDeSemana && !eFeriado)
            {
                diasUteis++;
            }
            dataCorrente = dataCorrente.AddDays(1);
        }

        return eOverdue ? -diasUteis : diasUteis;
    }
}
