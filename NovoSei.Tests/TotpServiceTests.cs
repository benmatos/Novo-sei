using System;
using NovoSei.Core.Services;
using Xunit;

namespace NovoSei.Tests;

public class TotpServiceTests
{
    [Fact]
    public void GerarSegredoBase32_DeveRetornarStringValida()
    {
        // Arrange
        var service = new TotpService();

        // Act
        var secret = service.GerarSegredoBase32();

        // Assert
        Assert.NotNull(secret);
        Assert.Equal(10, secret.Length);
        
        // Deve conter apenas caracteres válidos do alfabeto Base32
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        foreach (char c in secret)
        {
            Assert.Contains(c, alphabet);
        }
    }

    [Fact]
    public void GerarQrCodeUri_DeveRetornarUriValida()
    {
        // Arrange
        var service = new TotpService();
        string email = "test@example.com";
        string secret = "JBSWY3DPEHPK3PXP";

        // Act
        var uri = service.GerarQrCodeUri(email, secret);

        // Assert
        Assert.NotNull(uri);
        Assert.Contains("otpauth://totp/", uri);
        Assert.Contains("NovoSEI", uri);
        Assert.Contains(Uri.EscapeDataString(email), uri);
        Assert.Contains(secret, uri);
    }

    [Fact]
    public void ValidarCodigo_ComCodigoCorreto_DeveRetornarTrue()
    {
        // Arrange
        var service = new TotpService();
        var secret = service.GerarSegredoBase32();

        // Act
        var code = service.GerarCodigoAtual(secret);
        var isValid = service.ValidarCodigo(secret, code);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidarCodigo_ComCodigoIncorreto_DeveRetornarFalse()
    {
        // Arrange
        var service = new TotpService();
        var secret = service.GerarSegredoBase32();

        // Act
        var isValid = service.ValidarCodigo(secret, "999999"); // Código falso

        // Assert
        Assert.False(isValid);
    }
}
