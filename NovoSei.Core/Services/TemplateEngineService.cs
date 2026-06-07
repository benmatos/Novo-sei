using System;
using NovoSei.Core.Interfaces;

namespace NovoSei.Core.Services;

public class TemplateEngineService : ITemplateEngineService
{
    public string ProcessarTemplate(string htmlBase, string numeroProcesso, string textoConteudo)
    {
        if (string.IsNullOrEmpty(htmlBase))
            return string.Empty;

        return htmlBase
            .Replace("{{NumeroProcesso}}", numeroProcesso ?? string.Empty)
            .Replace("{{DataAtual}}", DateTime.Now.ToString("dd/MM/yyyy"))
            .Replace("{{TextoConteudo}}", textoConteudo ?? string.Empty);
    }
}
