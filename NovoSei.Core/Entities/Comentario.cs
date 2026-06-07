using System;

namespace NovoSei.Core.Entities;

public class Comentario
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public int? ProcessoId { get; set; }
    public Processo? Processo { get; set; }

    public int? DocumentoId { get; set; }
    public Documento? Documento { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
}
