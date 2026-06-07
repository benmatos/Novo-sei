using System.Threading.Tasks;

namespace NovoSei.Core.Interfaces;

public interface INotificationService
{
    Task NotificarDocumentoAssinadoAsync(int documentoId, string tituloDocumento, string nomeUsuario);
    Task NotificarNovoProcessoAsync(int processoId, string numeroSequencial, string assunto);
}
