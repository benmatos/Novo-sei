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
                sql => sql.MigrationsAssembly("NovoSei.Infrastructure")
                          .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILdapAuthenticationService, LdapAuthenticationService>();
        services.AddScoped<ITemplateEngineService, NovoSei.Core.Services.TemplateEngineService>();
        services.AddScoped<IDocumentoService, NovoSei.Infrastructure.Services.DocumentoService>();
        services.AddScoped<IDashboardService, NovoSei.Infrastructure.Services.DashboardService>();

        if (configuration.GetValue<bool>("Storage:UseS3"))
        {
            services.AddScoped<IFileStorageService, NovoSei.Infrastructure.Services.S3FileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, NovoSei.Infrastructure.Services.LocalFileStorageService>();
        }

        // Configuração de Caching Distribuído (Redis ou InMemory Fallback)
        if (configuration.GetValue<bool>("Caching:UseRedis"))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Caching:Redis:ConnectionString"] ?? "localhost:6379";
                options.InstanceName = "NovoSei:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<INotificationService, NovoSei.Web.Services.SignalRNotificationService>();
        services.AddHttpClient();
        services.AddScoped<IAssistenteService, NovoSei.Infrastructure.Services.LlamaAiService>();
        services.AddScoped<IIngestaoLegacyService, NovoSei.Infrastructure.Services.IngestaoLegacyService>();
        services.AddScoped<IMarcadorService, NovoSei.Infrastructure.Services.MarcadorService>();
        services.AddScoped<IComentarioService, NovoSei.Infrastructure.Services.ComentarioService>();
        services.AddScoped<ITotpService, NovoSei.Core.Services.TotpService>();
        services.AddScoped<ISlaCalculatorService, NovoSei.Infrastructure.Services.SlaCalculatorService>();
        services.AddScoped<IControlePrazoService, NovoSei.Infrastructure.Services.ControlePrazoService>();
        services.AddScoped<IBlocoReuniaoService, NovoSei.Infrastructure.Services.BlocoReuniaoService>();

        return services;
    }

    public static IServiceCollection AddNovoSeiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var useOidc = configuration.GetValue<bool>("Authentication:UseOidc");

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            if (useOidc)
            {
                options.DefaultChallengeScheme = "OpenIdConnect";
            }
        });

        authBuilder.AddCookie(options =>
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

        if (useOidc)
        {
            authBuilder.AddOpenIdConnect("OpenIdConnect", options =>
            {
                options.Authority = configuration["Authentication:Oidc:Authority"];
                options.ClientId = configuration["Authentication:Oidc:ClientId"];
                options.ClientSecret = configuration["Authentication:Oidc:ClientSecret"];
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });
        }

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
