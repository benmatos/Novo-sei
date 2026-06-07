using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NovoSei.Core.Entities;
using NovoSei.Infrastructure.Data;
using NovoSei.Infrastructure.Repositories;
using Xunit;

namespace NovoSei.Tests;

[Collection("DatabaseTests")]
public class Auth2FaRepositoryTests
{
    private ApplicationDbContext ObterContextoLocalDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovoSeiDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UsuarioRepository_Atualizar2FaAsync_DevePersistirCamposMfaCorretamente()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var repo = new UsuarioRepository(db);

        // Criar um usuário de teste exclusivo
        var loginUnico = $"user2fa_{Guid.NewGuid().ToString()[..6]}";
        var usuario = new Usuario
        {
            Nome = "Usuário Teste 2FA",
            Login = loginUnico,
            Email = $"{loginUnico}@novosei.gov.br",
            Perfil = "UsuarioComum"
        };
        await repo.CriarAsync(usuario);

        // Act & Assert 1: Validar valores padrões de 2FA
        var usuarioCriado = await repo.ObterPorLoginAsync(loginUnico);
        Assert.NotNull(usuarioCriado);
        Assert.False(usuarioCriado.DoisFatoresHabilitado);
        Assert.Null(usuarioCriado.Segredo2Fa);
        Assert.Null(usuarioCriado.EmailAlternativo);
        Assert.Null(usuarioCriado.Token2FaAtivacao);
        Assert.Null(usuarioCriado.TokenEmailExpiracao);

        // Act 2: Atualizar configurações de 2FA
        string segredo = "JBSWY3DPEHPK3PXP";
        string emailAlternativo = "alternativo@example.com";
        string tokenAtivacao = "token_ativacao_123";
        DateTime expiracao = DateTime.UtcNow.AddHours(1);

        await repo.Atualizar2FaAsync(usuarioCriado.Id, true, segredo, emailAlternativo, tokenAtivacao, expiracao);

        // Assert 2: Obter novamente e validar se foi persistido
        var usuarioAtualizado = await repo.ObterPorLoginAsync(loginUnico);
        Assert.NotNull(usuarioAtualizado);
        Assert.True(usuarioAtualizado.DoisFatoresHabilitado);
        Assert.Equal(segredo, usuarioAtualizado.Segredo2Fa);
        Assert.Equal(emailAlternativo, usuarioAtualizado.EmailAlternativo);
        Assert.Equal(tokenAtivacao, usuarioAtualizado.Token2FaAtivacao);
        Assert.NotNull(usuarioAtualizado.TokenEmailExpiracao);
        
        // Tolerância de comparação de datas por causa da persistência
        Assert.True((usuarioAtualizado.TokenEmailExpiracao.Value - expiracao).Duration() < TimeSpan.FromSeconds(5));

        // Limpar usuário de teste
        db.ChangeTracker.Clear();
        db.Usuarios.Remove(usuarioCriado);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LdapAuthenticationService_FallbackLocal_DeveAutenticarAdminComSucesso()
    {
        // Arrange
        using var db = ObterContextoLocalDb();
        var repo = new UsuarioRepository(db);
        
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Ldap:Host"]).Returns("ldap.caixa.gov.br");
        mockConfig.Setup(c => c["Ldap:Port"]).Returns("389");
        mockConfig.Setup(c => c["Ldap:BaseDn"]).Returns("DC=caixa,DC=gov,DC=br");
        mockConfig.Setup(c => c["Ldap:Domain"]).Returns("caixa.gov.br");

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NovoSei.Infrastructure.Services.LdapAuthenticationService>.Instance;
        var service = new NovoSei.Infrastructure.Services.LdapAuthenticationService(mockConfig.Object, repo, logger);

        // Act
        var result = await service.AutenticarAsync("admin", "admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Login);
        Assert.Equal("Administrador", result.Perfil);

        // Verificar se o usuário foi criado no banco
        var userInDb = await repo.ObterPorLoginAsync("admin");
        Assert.NotNull(userInDb);
        Assert.Equal("admin", userInDb.Login);
    }
}
