namespace NovoSei.Core.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Perfil { get; set; } = "UsuarioComum";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcessoEm { get; set; }

    public ICollection<Processo> Processos { get; set; } = [];
    public ICollection<Assinatura> Assinaturas { get; set; } = [];
}
