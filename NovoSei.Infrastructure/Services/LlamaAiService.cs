using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NovoSei.Core.Interfaces;

namespace NovoSei.Infrastructure.Services;

public class LlamaAiService(IHttpClientFactory httpClientFactory) : IAssistenteService
{
    public async Task<string> SumarizarTextoAsync(string textoHtml)
    {
        if (string.IsNullOrEmpty(textoHtml))
            return "O documento está vazio e não pode ser sumarizado.";

        // Remover tags HTML do conteúdo para reduzir o número de tokens enviados
        var textoLimpo = Regex.Replace(textoHtml, "<.*?>", string.Empty).Trim();
        if (string.IsNullOrEmpty(textoLimpo))
            return "O documento está vazio e não pode ser sumarizado.";

        // Limita o texto para evitar estourar o tamanho do contexto do modelo local
        if (textoLimpo.Length > 4000)
        {
            textoLimpo = textoLimpo[..4000] + "... [Texto truncado para sumarização]";
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var requestBody = new
            {
                model = "qwen2.5-coder-1.5b-instruct-q4_k_m",
                messages = new[]
                {
                    new { role = "system", content = "Você é um assistente especializado no sistema SEI. Crie um resumo executivo objetivo e conciso do documento enviado, destacando os pontos principais em português brasileiro. Retorne apenas o resumo em formato de tópicos markdown, sem saudações, observações ou explicações." },
                    new { role = "user", content = $"Por favor, sumarize o seguinte documento:\n\n{textoLimpo}" }
                },
                temperature = 0.3,
                max_tokens = 400
            };

            var response = await client.PostAsJsonAsync("http://localhost:8080/v1/chat/completions", requestBody);
            if (!response.IsSuccessStatusCode)
            {
                return "Não foi possível conectar ao assistente de IA local para gerar o resumo (Servidor offline ou erro de resposta).";
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (jsonResult.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? "Falha ao extrair o texto do resumo.";
            }

            return "Formato de resposta inesperado do assistente de IA.";
        }
        catch (HttpRequestException)
        {
            return "O assistente de IA local (Llama Server) na porta 8080 não está respondendo. Certifique-se de que o servidor está ativo.";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado ao gerar resumo: {ex.Message}";
        }
    }
}
