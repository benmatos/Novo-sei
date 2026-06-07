using System;
using System.Collections.Generic;

namespace NovoSei.Core.Entities;

public class BlocoReuniao
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = "Aberto"; // Aberto, Disponibilizado, Concluido
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public int GeradoraUnidadeId { get; set; }
    public Unidade GeradoraUnidade { get; set; } = null!;

    public int CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPorUsuario { get; set; } = null!;

    // Processos incluídos no Bloco (M:N)
    public ICollection<Processo> Processos { get; set; } = [];

    // Detalhes do trâmite com as unidades receptoras (M:N com metadados)
    public ICollection<BlocoReuniaoUnidade> BlocoUnidades { get; set; } = [];
}
