using System;

namespace NovoSei.Core.Entities;

public class BlocoReuniaoUnidade
{
    public int BlocoReuniaoId { get; set; }
    public BlocoReuniao BlocoReuniao { get; set; } = null!;

    public int UnidadeReceptoraId { get; set; }
    public Unidade UnidadeReceptora { get; set; } = null!;

    public string Status { get; set; } = "Disponibilizado"; // Disponibilizado, Devolvido
    public DateTime? DevolvidoEm { get; set; }
}
