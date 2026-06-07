using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class SlaCalculatorServiceTests
{
    private ApplicationDbContext ObterContextoLocalDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovoSeiDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CalcularDiasRestantesAsync_DiasCorridos_DeveCalcularDiferencaSimples()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new SlaCalculatorService(db);
        var limite = DateTime.Today.AddDays(5);

        // Act
        var dias = await service.CalcularDiasRestantesAsync(limite, contarDiasUteis: false);

        // Assert
        Assert.Equal(5, dias);
    }

    [Fact]
    public async Task CalcularDiasRestantesAsync_DiasCorridosAtrasado_DeveRetornarNegativo()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new SlaCalculatorService(db);
        var limite = DateTime.Today.AddDays(-3);

        // Act
        var dias = await service.CalcularDiasRestantesAsync(limite, contarDiasUteis: false);

        // Assert
        Assert.Equal(-3, dias);
    }

    [Fact]
    public async Task CalcularDiasRestantesAsync_DiasUteisSemFeriados_DeveExcluirFinsDeSemana()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new SlaCalculatorService(db);

        // Achar uma sexta-feira para testar
        var sexta = DateTime.Today;
        while (sexta.DayOfWeek != DayOfWeek.Friday)
        {
            sexta = sexta.AddDays(1);
        }

        // Limite na segunda-feira seguinte (+3 dias corridos, mas apenas 1 dia útil: a segunda-feira)
        var segunda = sexta.AddDays(3);

        // Act
        // Do ponto de vista de hoje = sexta, limite = segunda
        // Ao rodar em datas dinâmicas, podemos simular definindo a data.
        // Como o serviço usa DateTime.Today internamente, vamos testar com base no dia atual do servidor.
        // Vamos forçar a execução a partir do dia atual de forma controlada.
        
        var hoje = DateTime.Today;
        var limite = hoje;
        int diasUteisEsperados = 0;
        
        // Vamos caminhar dia a dia e calcular manualmente o esperado para bater com o serviço
        for (int i = 1; i <= 7; i++)
        {
            var dia = hoje.AddDays(i);
            if (dia.DayOfWeek != DayOfWeek.Saturday && dia.DayOfWeek != DayOfWeek.Sunday)
            {
                diasUteisEsperados++;
            }
            limite = dia;
        }

        // Act
        var dias = await service.CalcularDiasRestantesAsync(limite, contarDiasUteis: true);

        // Assert
        Assert.Equal(diasUteisEsperados, dias);
    }

    [Fact]
    public async Task CalcularDiasRestantesAsync_DiasUteisComFeriado_DeveExcluirFeriados()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var service = new SlaCalculatorService(db);

        var hoje = DateTime.Today;
        
        // Encontrar um dia da semana (segunda a sexta) para ser nosso feriado nos próximos 4 dias
        DateTime? diaFeriado = null;
        DateTime limite = hoje;
        int diasUteisEsperados = 0;

        for (int i = 1; i <= 4; i++)
        {
            var dia = hoje.AddDays(i);
            bool eFimDeSemana = dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday;
            
            if (!eFimDeSemana && diaFeriado == null)
            {
                diaFeriado = dia;
                // Como é feriado, não deve ser contado nos dias úteis esperados
            }
            else if (!eFimDeSemana)
            {
                diasUteisEsperados++;
            }
            limite = dia;
        }

        Assert.NotNull(diaFeriado); // Garante que encontramos um dia útil para registrar feriado

        // Cadastrar feriado
        var feriado = new Feriado { Data = diaFeriado.Value, Descricao = "Feriado de Teste SLA" };
        db.Feriados.Add(feriado);
        await db.SaveChangesAsync();

        try
        {
            // Act
            var dias = await service.CalcularDiasRestantesAsync(limite, contarDiasUteis: true);

            // Assert
            Assert.Equal(diasUteisEsperados, dias);
        }
        finally
        {
            // Clean up
            db.Feriados.Remove(feriado);
            await db.SaveChangesAsync();
        }
    }
}
