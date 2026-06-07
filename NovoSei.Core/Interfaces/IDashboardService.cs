using NovoSei.Core.DTOs;

namespace NovoSei.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> ObterIndicadoresAsync(int usuarioId);
    Task InvalidarCacheAsync(int usuarioId);
}
