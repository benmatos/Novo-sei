using System.IO;
using Microsoft.Extensions.Configuration;
using NovoSei.Core.Interfaces;

namespace NovoSei.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["Storage:LocalPath"] ?? @"C:\NovoSei\Storage";
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo)
    {
        NovoSei.Core.Services.FileSecurityValidator.ValidarArquivo(nomeArquivo, conteudo);
        var caminhoCompleto = Path.Combine(_storagePath, nomeArquivo);
        await File.WriteAllBytesAsync(caminhoCompleto, conteudo);
        return caminhoCompleto;
    }

    public async Task<byte[]?> ObterArquivoAsync(string caminho)
    {
        if (!File.Exists(caminho))
            return null;

        return await File.ReadAllBytesAsync(caminho);
    }

    public Task DeletarArquivoAsync(string caminho)
    {
        if (File.Exists(caminho))
        {
            File.Delete(caminho);
        }
        return Task.CompletedTask;
    }
}
