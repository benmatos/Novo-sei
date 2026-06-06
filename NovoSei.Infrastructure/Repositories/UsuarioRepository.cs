using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Repositories;

public class UsuarioRepository(ApplicationDbContext db) : IUsuarioRepository
{
    public async Task<Usuario?> ObterPorLoginAsync(string login) =>
        await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == login);

    public async Task<Usuario> CriarAsync(Usuario usuario)
    {
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task AtualizarUltimoAcessoAsync(int usuarioId)
    {
        await db.Usuarios
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoAcessoEm, DateTime.UtcNow));
    }
}
