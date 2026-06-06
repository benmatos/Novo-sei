namespace NovoSei.Core.Entities;

public class Documento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string ConteudoHtml { get; set; } = string.Empty;
    public string Status { get; set; } = "Rascunho";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public string? CaminhoArquivoPdf { get; set; }

    public int ProcessoId { get; set; }
    public Processo Processo { get; set; } = null!;

    public int TemplateDocumentoId { get; set; }
    public TemplateDocumento TemplateDocumento { get; set; } = null!;

    public ICollection<Assinatura> Assinaturas { get; set; } = [];
}
