using System;
using NovoSei.Core.Services;
using Xunit;

namespace NovoSei.Tests;

public class FileSecurityValidatorTests
{
    [Fact]
    public void ValidarArquivo_ComPdfValido_DevePassar()
    {
        // %PDF-1.4 header
        byte[] conteudoPdfValido = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
        
        // Act & Assert
        FileSecurityValidator.ValidarArquivo("documento.pdf", conteudoPdfValido);
    }

    [Fact]
    public void ValidarArquivo_ComPdfInvalido_DeveLancarExcecao()
    {
        byte[] conteudoPdfInvalido = [0x00, 0x11, 0x22, 0x33, 0x44];
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            FileSecurityValidator.ValidarArquivo("documento.pdf", conteudoPdfInvalido));
    }

    [Fact]
    public void ValidarArquivo_ComExtensaoProibida_DeveLancarExcecao()
    {
        byte[] conteudoQualquer = [0x12, 0x34, 0x56];
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            FileSecurityValidator.ValidarArquivo("teste.exe", conteudoQualquer));
    }

    [Fact]
    public void ValidarArquivo_ComCabecalhoMZMasExtensaoValida_DeveLancarExcecao()
    {
        // Inicia com MZ (executável PE), disfarçado de PDF
        byte[] conteudoFakePdf = [0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00];
        
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            FileSecurityValidator.ValidarArquivo("documento_fake.pdf", conteudoFakePdf));
        Assert.Contains("executável (MZ) detectada", ex.Message);
    }

    [Fact]
    public void ValidarArquivo_ComPngValido_DevePassar()
    {
        byte[] conteudoPngValido = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        
        // Act & Assert
        FileSecurityValidator.ValidarArquivo("foto.png", conteudoPngValido);
    }

    [Fact]
    public void ValidarArquivo_ComPngInvalido_DeveLancarExcecao()
    {
        byte[] conteudoPngInvalido = [0x89, 0x50, 0x4E, 0x47, 0x00, 0x00, 0x00, 0x00];
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            FileSecurityValidator.ValidarArquivo("foto.png", conteudoPngInvalido));
    }
}
