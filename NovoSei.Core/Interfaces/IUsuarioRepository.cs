using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorLoginAsync(string login);
    Task<Usuario> CriarAsync(Usuario usuario);
    Task AtualizarUltimoAcessoAsync(int usuarioId);
}
