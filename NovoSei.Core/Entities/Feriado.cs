using System;

namespace NovoSei.Core.Entities;

public class Feriado
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
