using System.Collections.Generic;
using System.Threading.Tasks;
using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public interface IMarcadorService
{
    Task<List<Marcador>> ObterMarcadoresDaUnidadeAsync(int unidadeId);
    Task<Marcador> CriarMarcadorAsync(int unidadeId, string nome, string corHex);
    Task<Marcador?> ObterMarcadorPorIdAsync(int id);
    Task<bool> AtualizarMarcadorAsync(int id, string nome, string corHex);
    Task<bool> AlternarAtivoMarcadorAsync(int id, bool ativo);
    Task<bool> ExcluirMarcadorAsync(int id);
    Task<bool> AssociarMarcadorAoProcessoAsync(int processoId, int marcadorId);
    Task<bool> DesassociarMarcadorDoProcessoAsync(int processoId, int marcadorId);
    Task<bool> AssociarMarcadoresAoProcessoEmLoteAsync(List<int> processosIds, int marcadorId);
}
