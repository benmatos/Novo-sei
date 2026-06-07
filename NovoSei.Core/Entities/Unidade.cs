using System.Collections.Generic;

namespace NovoSei.Core.Entities;

public class Unidade
{
    public int Id { get; set; }
    public string Sigla { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public int OrgaoId { get; set; }
    public Orgao Orgao { get; set; } = null!;

    public int? ParentUnidadeId { get; set; }
    public Unidade? ParentUnidade { get; set; }
    public ICollection<Unidade> ChildUnidades { get; set; } = [];

    public ICollection<Usuario> Usuarios { get; set; } = [];
    public ICollection<Processo> Processos { get; set; } = [];
}
