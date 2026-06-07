using System.Threading.Tasks;

namespace NovoSei.Core.Interfaces;

public record IngestaoResult(
    int UsuariosImportados, 
    int ProcessosImportados, 
    int DocumentosImportados, 
    int AssinaturasImportados, 
    bool Sucesso, 
    string Mensagem
);

public record IngestaoStatsDto(
    int TotalUsuariosLegado, 
    int TotalProcessosLegado, 
    int TotalDocumentosLegado, 
    int TotalAssinaturasLegado
);

public interface IIngestaoLegacyService
{
    Task<IngestaoResult> IngerirDadosLegadosAsync();
    Task<IngestaoStatsDto> ObterEstatisticasLegadoAsync();
    Task LimparDadosLocaisAsync();
}
