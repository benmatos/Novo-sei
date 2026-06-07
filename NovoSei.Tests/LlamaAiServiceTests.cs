using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NovoSei.Infrastructure.Services;
using Xunit;

namespace NovoSei.Tests;

public class LlamaAiServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncFunc { get; set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncFunc(request);
        }
    }

    [Fact]
    public async Task SumarizarTextoAsync_DeveRetornarMensagemSeVazio()
    {
        // Arrange
        var mockFactory = new Mock<IHttpClientFactory>();
        var service = new LlamaAiService(mockFactory.Object);

        // Act
        var result = await service.SumarizarTextoAsync("");

        // Assert
        Assert.Equal("O documento está vazio e não pode ser sumarizado.", result);
    }

    [Fact]
    public async Task SumarizarTextoAsync_DeveLimparHtmlEPegarTexto()
    {
        // Arrange
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new MockHttpMessageHandler();
        var client = new HttpClient(mockHandler);
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new LlamaAiService(mockFactory.Object);
        var htmlContent = "<p>Documento de <strong>teste</strong> muito importante.</p>";
        
        string? requestBodyText = null;

        mockHandler.SendAsyncFunc = async (req) =>
        {
            requestBodyText = await req.Content!.ReadAsStringAsync();
            
            var responseJson = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "Resumo em tópicos:\n- Item 1\n- Item 2"
                        }
                    }
                }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseJson)
            };
        };

        // Act
        var result = await service.SumarizarTextoAsync(htmlContent);

        // Assert
        Assert.NotNull(requestBodyText);
        Assert.Contains("Documento de teste muito importante.", requestBodyText);
        Assert.DoesNotContain("<p>", requestBodyText);
        Assert.Contains("Resumo em tópicos:", result);
    }

    [Fact]
    public async Task SumarizarTextoAsync_DeveRetornarMensagemErroAoFalharHttp()
    {
        // Arrange
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new MockHttpMessageHandler();
        var client = new HttpClient(mockHandler);
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new LlamaAiService(mockFactory.Object);

        mockHandler.SendAsyncFunc = (req) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        };

        // Act
        var result = await service.SumarizarTextoAsync("<p>Texto teste</p>");

        // Assert
        Assert.Contains("Não foi possível conectar ao assistente de IA local", result);
    }
}
