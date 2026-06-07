using System.Threading.Tasks;

namespace NovoSei.Core.Interfaces;

public interface IAssistenteService
{
    Task<string> SumarizarTextoAsync(string textoHtml);
}
