using System.Collections.Generic;

namespace NovoSei.Core.Entities;

public class Orgao
{
    public int Id { get; set; }
    public string Sigla { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public ICollection<Unidade> Unidades { get; set; } = [];
}
