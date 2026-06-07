using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovoSei.Core.Interfaces;

namespace NovoSei.Web.Services;

public class IngestaoBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IngestaoBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ativa = configuration.GetValue<bool>("Ingestao:Ativa");
        var intervaloMinutos = configuration.GetValue<int>("Ingestao:IntervaloMinutos");
        
        if (intervaloMinutos <= 0) intervaloMinutos = 10; // Fallback para 10 minutos

        if (!ativa)
        {
            logger.LogInformation("O Worker de Ingestão Periódica está desativado nas configurações (Ingestao:Ativa = false).");
            return;
        }

        logger.LogInformation("Worker de Ingestão Periódica iniciado. Executando a cada {Intervalo} minutos.", intervaloMinutos);

        // Aguarda 5 segundos antes da primeira execução para garantir o boot completo do app
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Iniciando execução periódica de ingestão de dados legados...");

            try
            {
                using var scope = scopeFactory.CreateScope();
                var ingestaoService = scope.ServiceProvider.GetRequiredService<IIngestaoLegacyService>();
                
                var result = await ingestaoService.IngerirDadosLegadosAsync();
                
                if (result.Sucesso)
                {
                    logger.LogInformation("Ingestão periódica concluída com sucesso: " +
                                          "[Usuários Importados: {Users}, Processos Importados: {Procs}, " +
                                          "Documentos Importados: {Docs}, Assinaturas Importadas: {Sigs}]",
                        result.UsuariosImportados, result.ProcessosImportados, result.DocumentosImportados, result.AssinaturasImportados);
                }
                else
                {
                    logger.LogWarning("Ingestão periódica executada, mas retornou falha: {Mensagem}", result.Mensagem);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado durante a execução periódica de ingestão.");
            }

            // Aguarda o intervalo antes de executar novamente
            try
            {
                logger.LogInformation("Aguardando {Intervalo} minutos para a próxima execução da ingestão...", intervaloMinutos);
                await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Worker de Ingestão Periódica finalizado.");
    }
}
