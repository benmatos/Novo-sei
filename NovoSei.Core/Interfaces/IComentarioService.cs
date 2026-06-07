using System.Collections.Generic;
using System.Threading.Tasks;
using NovoSei.Core.Entities;

namespace NovoSei.Core.Interfaces;

public interface IComentarioService
{
    Task<List<Comentario>> ObterComentariosDoProcessoAsync(int processoId, int usuarioId, int unidadeId);
    Task<List<Comentario>> ObterComentariosDoDocumentoAsync(int documentoId, int usuarioId, int unidadeId);
    Task<Comentario> CriarComentarioProcessoAsync(int processoId, string descricao, int usuarioId, int unidadeId);
    Task<Comentario> CriarComentarioDocumentoAsync(int documentoId, string descricao, int usuarioId, int unidadeId);
    Task<bool> ExcluirComentarioAsync(int comentarioId, int unidadeId);
    Task<bool> AlterarComentarioAsync(int comentarioId, string descricao, int unidadeId);
}
