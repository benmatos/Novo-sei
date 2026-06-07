using Microsoft.AspNetCore.SignalR;
using NovoSei.Core.Interfaces;
using NovoSei.Web.Hubs;
using System.Threading.Tasks;

namespace NovoSei.Web.Services;

public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task NotificarDocumentoAssinadoAsync(int documentoId, string tituloDocumento, string nomeUsuario)
    {
        await hubContext.Clients.All.SendAsync("ReceberNotificacaoDocumentoAssinado", new
        {
            DocumentoId = documentoId,
            Titulo = tituloDocumento,
            Usuario = nomeUsuario,
            Mensagem = $"O documento '{tituloDocumento}' foi assinado eletronicamente por {nomeUsuario}."
        });
    }

    public async Task NotificarNovoProcessoAsync(int processoId, string numeroSequencial, string assunto)
    {
        await hubContext.Clients.All.SendAsync("ReceberNotificacaoNovoProcesso", new
        {
            ProcessoId = processoId,
            NumeroSequencial = numeroSequencial,
            Assunto = assunto,
            Mensagem = $"Novo processo autuado: {numeroSequencial} - {assunto}."
        });
    }
}
