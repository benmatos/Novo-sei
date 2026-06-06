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
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Senha))
                return Results.BadRequest("Login e senha são obrigatórios.");

            var loginResult = await ldapService.AutenticarAsync(request.Login, request.Senha);

            if (loginResult is null)
                return Results.Unauthorized();

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

            return Results.Ok(loginResult);
        })
        .AllowAnonymous()
        .WithName("Login")
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
}
