namespace NovoSei.Core.DTOs;

public record DashboardDto(
    int TotalProcessos,
    int ProcessosAbertos,
    int RascunhosPendentes,
    int TramitesRealizados
);
