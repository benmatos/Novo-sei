using System;
using System.Threading.Tasks;

namespace NovoSei.Core.Interfaces;

public interface ISlaCalculatorService
{
    Task<int> CalcularDiasRestantesAsync(DateTime dataLimite, bool contarDiasUteis);
}
