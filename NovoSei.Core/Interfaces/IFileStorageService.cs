namespace NovoSei.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo);
    Task<byte[]?> ObterArquivoAsync(string caminho);
    Task DeletarArquivoAsync(string caminho);
}
