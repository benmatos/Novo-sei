using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public interface IControlePrazoService
{
    Task<ControlePrazo> CriarPrazoAsync(int processoId, int unidadeId, DateTime dataLimite, bool diasUteis, int criadoPorUsuarioId);
    Task<bool> ConcluirPrazoAsync(int prazoId, int resolvidoPorUsuarioId);
    Task<bool> RemoverPrazoAsync(int prazoId);
    Task<List<ControlePrazo>> ObterPrazosAtivosUnidadeAsync(int unidadeId);
    Task<ControlePrazo?> ObterPrazoAtivoProcessoUnidadeAsync(int processoId, int unidadeId);
}
