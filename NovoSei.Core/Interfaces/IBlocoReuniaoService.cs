using System.Collections.Generic;
using System.Threading.Tasks;
using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public interface IBlocoReuniaoService
{
    Task<BlocoReuniao> CriarBlocoAsync(string descricao, int geradoraUnidadeId, int criadoPorUsuarioId);
    Task<bool> AdicionarProcessosAoBlocoAsync(int blocoId, List<int> processoIds);
    Task<bool> RemoverProcessosDoBlocoAsync(int blocoId, List<int> processoIds);
    Task<bool> DisponibilizarBlocoAsync(int blocoId, List<int> unidadeReceptoraIds);
    Task<bool> CancelarDisponibilizacaoAsync(int blocoId);
    Task<bool> DevolverBlocoAsync(int blocoId, int unidadeReceptoraId);
    Task<bool> ConcluirBlocoAsync(int blocoId);
    Task<List<BlocoReuniao>> ObterBlocosGeradosUnidadeAsync(int unidadeId);
    Task<List<BlocoReuniao>> ObterBlocosRecebidosUnidadeAsync(int unidadeId);
    Task<BlocoReuniao?> ObterBlocoComDetalhesAsync(int blocoId);
}
