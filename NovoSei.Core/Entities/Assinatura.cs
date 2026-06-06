namespace NovoSei.Core.Entities;

public class Assinatura
{
    public int Id { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public DateTime AssinadoEm { get; set; } = DateTime.UtcNow;

    public int DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
