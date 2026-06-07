using Xunit;
using NovoSei.Core.Services;

namespace NovoSei.Tests;

public class TemplateEngineServiceTests
{
    [Fact]
    public void ProcessarTemplate_DeveSubstituirPlaceholdersCorretamente()
    {
        // Arrange
        var service = new TemplateEngineService();
        var htmlBase = "Processo: {{NumeroProcesso}} | Data: {{DataAtual}} | Conteudo: {{TextoConteudo}}";
        var numeroProcesso = "SEI-12345";
        var textoConteudo = "Texto de Teste";
        var dataEsperada = DateTime.Now.ToString("dd/MM/yyyy");

        // Act
        var resultado = service.ProcessarTemplate(htmlBase, numeroProcesso, textoConteudo);

        // Assert
        Assert.Contains(numeroProcesso, resultado);
        Assert.Contains(dataEsperada, resultado);
        Assert.Contains(textoConteudo, resultado);
    }

    [Fact]
    public void ProcessarTemplate_DeveRetornarVazio_SeHtmlBaseForNuloOuVazio()
    {
        // Arrange
        var service = new TemplateEngineService();

        // Act
        var resultado = service.ProcessarTemplate(null!, "SEI-123", "Conteudo");

        // Assert
        Assert.Equal(string.Empty, resultado);
    }
}
