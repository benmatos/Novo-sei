using System;

namespace NovoSei.Core.Entities;

public class ControlePrazo
{
    public int Id { get; set; }
    
    public int ProcessoId { get; set; }
    public Processo Processo { get; set; } = null!;
    
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    
    public DateTime DataLimite { get; set; }
    public bool DiasUteis { get; set; }
    
    public int CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPorUsuario { get; set; } = null!;
    
    public string Status { get; set; } = "Ativo"; // Ativo, Concluido, Removido
    
    public DateTime? ResolvidoEm { get; set; }
    public int? ResolvidoPorUsuarioId { get; set; }
    public Usuario? ResolvidoPorUsuario { get; set; }
}
