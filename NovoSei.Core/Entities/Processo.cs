namespace NovoSei.Core.Entities;

public class Processo
{
    public int Id { get; set; }
    public string NumeroSequencial { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Status { get; set; } = "Aberto";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EncerradoEm { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public ICollection<Documento> Documentos { get; set; } = [];
}
