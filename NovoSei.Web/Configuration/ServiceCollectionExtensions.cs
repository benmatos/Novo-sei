using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Repositories;
using NovoSei.Infrastructure.Services;

namespace NovoSei.Web.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("NovoSei.Infrastructure")));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILdapAuthenticationService, LdapAuthenticationService>();

        return services;
    }

    public static IServiceCollection AddNovoSeiAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/api/auth/logout";
                options.AccessDeniedPath = "/acesso-negado";
                options.Cookie.Name = "NovoSei.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Administrador", p => p.RequireRole("Administrador"));
            options.AddPolicy("Gestor", p => p.RequireRole("Administrador", "Gestor"));
            options.AddPolicy("UsuarioComum", p => p.RequireRole("Administrador", "Gestor", "UsuarioComum"));
        });

        services.AddCascadingAuthenticationState();

        return services;
    }
}
