namespace NovoSei.Core.Interfaces;

public interface ITemplateEngineService
{
    string ProcessarTemplate(string htmlBase, string numeroProcesso, string textoConteudo);
}
