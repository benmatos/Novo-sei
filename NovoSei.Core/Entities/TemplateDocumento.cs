namespace NovoSei.Core.Entities;

public class TemplateDocumento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string ConteudoHtmlBase { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Documento> Documentos { get; set; } = [];
}
