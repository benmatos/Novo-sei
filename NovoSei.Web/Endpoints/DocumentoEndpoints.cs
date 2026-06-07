using NovoSei.Core.Interfaces;

namespace NovoSei.Web.Endpoints;

public static class DocumentoEndpoints
{
    public static IEndpointRouteBuilder MapDocumentoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documentos/{id:int}/download", async (
            int id,
            IDocumentoService documentoService) =>
        {
            var bytes = await documentoService.ObterPdfBytesAsync(id);
            if (bytes is null)
                return Results.NotFound("Arquivo PDF não encontrado ou não foi gerado ainda.");

            return Results.File(bytes, "application/pdf", $"Documento_{id}.pdf");
        })
        .RequireAuthorization()
        .WithName("DownloadPdf")
        .WithTags("Documentos");

        return app;
    }
}
