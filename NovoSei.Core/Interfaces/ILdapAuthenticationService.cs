using NovoSei.Core.DTOs;

namespace NovoSei.Core.Interfaces;

public interface ILdapAuthenticationService
{
    Task<LoginResponse?> AutenticarAsync(string login, string senha);
}
