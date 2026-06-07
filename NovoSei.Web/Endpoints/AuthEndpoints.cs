using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using NovoSei.Core.DTOs;
using NovoSei.Core.Interfaces;

namespace NovoSei.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            ILdapAuthenticationService ldapService,
            IUsuarioRepository usuarioRepository,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Senha))
                return Results.BadRequest("Login e senha são obrigatórios.");

            var loginResult = await ldapService.AutenticarAsync(request.Login, request.Senha);

            if (loginResult is null)
                return Results.Unauthorized();

            // Verificar se o usuário tem 2FA ativado no banco
            var usuario = await usuarioRepository.ObterPorLoginAsync(loginResult.Login);
            if (usuario != null && usuario.DoisFatoresHabilitado)
            {
                // Verificar se o dispositivo é confiado
                var cookieName = $"NovoSei.TrustedDevice_{usuario.Login}";
                var deviceCookie = httpContext.Request.Cookies[cookieName];
                var expectedToken = GerarTokenDispositivoConfiado(usuario.Login, usuario.Segredo2Fa ?? "");

                if (deviceCookie != expectedToken)
                {
                    // MFA obrigatório
                    return Results.Ok(new
                    {
                        MfaRequired = true,
                        Login = usuario.Login
                    });
                }
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, loginResult.Id.ToString()),
                new(ClaimTypes.Name, loginResult.Nome),
                new(ClaimTypes.Email, loginResult.Email),
                new("login", loginResult.Login),
                new(ClaimTypes.Role, loginResult.Perfil)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return Results.Ok(new
            {
                MfaRequired = false,
                User = loginResult
            });
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithTags("Autenticação");

        app.MapPost("/api/auth/verify-2fa", async (
            Verify2FaRequest request,
            ILdapAuthenticationService ldapService,
            IUsuarioRepository usuarioRepository,
            ITotpService totpService,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Login) || 
                string.IsNullOrWhiteSpace(request.Senha) || 
                string.IsNullOrWhiteSpace(request.Codigo))
            {
                return Results.BadRequest("Login, senha e código de 6 dígitos são obrigatórios.");
            }

            // Validar credenciais via LDAP primeiro
            var loginResult = await ldapService.AutenticarAsync(request.Login, request.Senha);
            if (loginResult is null)
                return Results.Unauthorized();

            var usuario = await usuarioRepository.ObterPorLoginAsync(request.Login);
            if (usuario == null || !usuario.DoisFatoresHabilitado || string.IsNullOrEmpty(usuario.Segredo2Fa))
            {
                return Results.BadRequest("Autenticação de dois fatores não habilitada para este usuário.");
            }

            // Validar código de segurança TOTP
            var valid = totpService.ValidarCodigo(usuario.Segredo2Fa, request.Codigo);
            if (!valid)
            {
                return Results.BadRequest("Código de autenticação inválido ou expirado.");
            }

            // Configurar Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, loginResult.Id.ToString()),
                new(ClaimTypes.Name, loginResult.Nome),
                new(ClaimTypes.Email, loginResult.Email),
                new("login", loginResult.Login),
                new(ClaimTypes.Role, loginResult.Perfil)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            // Se solicitado, salvar cookie de dispositivo confiado
            if (request.ConfiarDispositivo)
            {
                var cookieName = $"NovoSei.TrustedDevice_{usuario.Login}";
                var token = GerarTokenDispositivoConfiado(usuario.Login, usuario.Segredo2Fa);
                httpContext.Response.Cookies.Append(cookieName, token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }

            return Results.Ok(new
            {
                MfaRequired = false,
                User = loginResult
            });
        })
        .AllowAnonymous()
        .WithName("Verify2Fa")
        .WithTags("Autenticação");

        app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Logout")
        .WithTags("Autenticação");

        app.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                Id = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Nome = user.FindFirstValue(ClaimTypes.Name),
                Email = user.FindFirstValue(ClaimTypes.Email),
                Login = user.FindFirstValue("login"),
                Perfil = user.FindFirstValue(ClaimTypes.Role)
            });
        })
        .RequireAuthorization()
        .WithName("MeusDados")
        .WithTags("Autenticação");

        return app;
    }

    private static string GerarTokenDispositivoConfiado(string login, string segredo)
    {
        var input = $"{login}:{segredo}:TrustedDeviceSalt";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
