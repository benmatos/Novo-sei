namespace NovoSei.Core.Entities;

public class Processo
{
    public int Id { get; set; }
    public string NumeroSequencial { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Status { get; set; } = "Aberto";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EncerradoEm { get; set; }

    public string Tipo { get; set; } = "Geral";
    public string? Interessados { get; set; }
    public string NivelAcesso { get; set; } = "Público";

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int? UnidadeId { get; set; }
    public Unidade? Unidade { get; set; }

    public ICollection<Documento> Documentos { get; set; } = [];
    public ICollection<Marcador> Marcadores { get; set; } = [];
    public ICollection<Comentario> Comentarios { get; set; } = [];
    public ICollection<ControlePrazo> ControlePrazos { get; set; } = [];
}
