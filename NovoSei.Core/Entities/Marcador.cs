using System.Collections.Generic;

namespace NovoSei.Core.Entities;

public class Marcador
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CorHex { get; set; } = "#6366F1"; // Indigo
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    public bool Ativo { get; set; } = true;

    // Relacionamento N:N com Processos
    public ICollection<Processo> Processos { get; set; } = [];
}
