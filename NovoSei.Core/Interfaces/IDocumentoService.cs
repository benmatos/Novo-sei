using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public record ProcessoVersaoDto(
    string NumeroSequencial,
    string Assunto,
    string Status,
    int UsuarioId,
    string UsuarioNome,
    DateTime PeriodStart,
    DateTime PeriodEnd
);

public record DocumentoVersaoDto(
    int Id,
    string Titulo,
    string ConteudoHtml,
    string Status,
    string? CaminhoArquivoPdf,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int? UnidadeId,
    string? UnidadeSigla
);

public interface IDocumentoService
{
    Task<Processo?> ObterProcessoComDocumentosAsync(int processoId);
    Task<Documento?> ObterDocumentoPorIdAsync(int documentoId);
    Task<Documento> CriarDocumentoAsync(int processoId, int templateId, string titulo, string textoConteudo);
    Task<bool> AssinarDocumentoAsync(int documentoId, string login, string senha);
    Task<byte[]?> ObterPdfBytesAsync(int documentoId);
    Task<List<ProcessoVersaoDto>> ObterHistoricoProcessoAsync(int processoId);
    Task<List<DocumentoVersaoDto>> ObterHistoricoDocumentoAsync(int documentoId);
}
