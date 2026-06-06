namespace NovoSei.Core.DTOs;

public record UsuarioDto(int Id, string Nome, string Email, string Login, string Perfil, DateTime CriadoEm, DateTime? UltimoAcessoEm);
