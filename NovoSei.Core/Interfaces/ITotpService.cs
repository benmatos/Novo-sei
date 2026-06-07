namespace NovoSei.Core.Interfaces;

public interface ITotpService
{
    string GerarSegredoBase32();
    string GerarQrCodeUri(string email, string segredo);
    string GerarCodigoAtual(string segredoBase32);
    bool ValidarCodigo(string segredoBase32, string codigo);
}
