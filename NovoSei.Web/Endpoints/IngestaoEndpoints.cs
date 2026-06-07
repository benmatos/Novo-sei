using NovoSei.Core.Interfaces;

namespace NovoSei.Web.Endpoints;

public static class IngestaoEndpoints
{
    public static IEndpointRouteBuilder MapIngestaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingestao")
            .RequireAuthorization()
            .WithTags("Ingestão Legacy");

        group.MapGet("/stats", async (IIngestaoLegacyService ingestaoService) =>
        {
            try
            {
                var stats = await ingestaoService.ObterEstatisticasLegadoAsync();
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Erro ao obter estatísticas da base legado: {ex.Message}");
            }
        });

        group.MapPost("/run", async (IIngestaoLegacyService ingestaoService) =>
        {
            try
            {
                var result = await ingestaoService.IngerirDadosLegadosAsync();
                if (result.Sucesso)
                {
                    return Results.Ok(result);
                }
                return Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Erro durante a execução da ingestão: {ex.Message}");
            }
        });

        group.MapPost("/clean-run", async (IIngestaoLegacyService ingestaoService) =>
        {
            try
            {
                await ingestaoService.LimparDadosLocaisAsync();
                var result = await ingestaoService.IngerirDadosLegadosAsync();
                if (result.Sucesso)
                {
                    return Results.Ok(result);
                }
                return Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Erro durante a execução da ingestão limpa: {ex.Message}");
            }
        });

        return app;
    }
}
