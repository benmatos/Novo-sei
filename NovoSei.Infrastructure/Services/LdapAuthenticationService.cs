using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovoSei.Core.DTOs;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;

namespace NovoSei.Infrastructure.Services;

public class LdapAuthenticationService(
    IConfiguration configuration,
    IUsuarioRepository usuarioRepository,
    ILogger<LdapAuthenticationService> logger) : ILdapAuthenticationService
{
    private readonly string _ldapHost = configuration["Ldap:Host"] ?? throw new InvalidOperationException("Ldap:Host não configurado.");
    private readonly int _ldapPort = int.Parse(configuration["Ldap:Port"] ?? "389");
    private readonly string _ldapBaseDn = configuration["Ldap:BaseDn"] ?? throw new InvalidOperationException("Ldap:BaseDn não configurado.");
    private readonly string _ldapDomain = configuration["Ldap:Domain"] ?? string.Empty;

    public async Task<LoginResponse?> AutenticarAsync(string login, string senha)
    {
        var userDn = string.IsNullOrEmpty(_ldapDomain)
            ? login
            : $"{login}@{_ldapDomain}";

        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_ldapHost, _ldapPort));
            connection.AuthType = AuthType.Basic;
            connection.SessionOptions.ProtocolVersion = 3;
            connection.Bind(new NetworkCredential(userDn, senha));

            var atributos = BuscarAtributosLdap(connection, login);

            var usuario = await usuarioRepository.ObterPorLoginAsync(login);

            if (usuario is null)
            {
                usuario = await usuarioRepository.CriarAsync(new Usuario
                {
                    Login = login,
                    Nome = atributos.GetValueOrDefault("displayName", login),
                    Email = atributos.GetValueOrDefault("mail", $"{login}@novosei.gov.br"),
                    Perfil = "UsuarioComum",
                    CriadoEm = DateTime.UtcNow
                });

                logger.LogInformation("Usuário {Login} auto-provisionado com sucesso.", login);
            }

            await usuarioRepository.AtualizarUltimoAcessoAsync(usuario.Id);

            return new LoginResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Login, usuario.Perfil);
        }
        catch (LdapException ex)
        {
            logger.LogWarning("Falha na autenticação LDAP para o usuário {Login}: {Mensagem}", login, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado durante autenticação LDAP para o usuário {Login}.", login);
            return null;
        }
    }

    private Dictionary<string, string> BuscarItributosLdap(LdapConnection connection, string login)
    {
        var resultado = new Dictionary<string, string>();

        try
        {
            var searchRequest = new SearchRequest(
                _ldapBaseDn,
                $"(sAMAccountName={login})",
                SearchScope.Subtree,
                "displayName", "mail", "givenName", "sn");

            if (connection.SendRequest(searchRequest) is not SearchResponse response)
                return resultado;

            foreach (SearchResultEntry entry in response.Entries)
            {
                foreach (string atributo in entry.Attributes.AttributeNames)
                {
                    var valores = entry.Attributes[atributo];
                    if (valores.Count > 0)
                        resultado[atributo] = valores[0]?.ToString() ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível recuperar atributos LDAP para o usuário {Login}.", login);
        }

        return resultado;
    }
}
