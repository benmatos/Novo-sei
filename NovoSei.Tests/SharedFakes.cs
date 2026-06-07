using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using NovoSei.Core.DTOs;
using NovoSei.Core.Interfaces;

namespace NovoSei.Tests;

public class FakeLdapService : ILdapAuthenticationService
{
    public LoginResponse? Retorno { get; set; }

    public Task<LoginResponse?> AutenticarAsync(string login, string senha)
    {
        return Task.FromResult(Retorno);
    }
}

public class FakeFileStorageService : IFileStorageService
{
    public Dictionary<string, byte[]> Arquivos { get; } = [];

    public Task<string> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo)
    {
        Arquivos[nomeArquivo] = conteudo;
        return Task.FromResult(nomeArquivo);
    }

    public Task<byte[]?> ObterArquivoAsync(string caminho)
    {
        Arquivos.TryGetValue(caminho, out var conteudo);
        return Task.FromResult(conteudo);
    }

    public Task DeletarArquivoAsync(string caminho)
    {
        Arquivos.Remove(caminho);
        return Task.CompletedTask;
    }
}

public class FakeDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _storage = [];

    public byte[]? Get(string key) => _storage.TryGetValue(key, out var val) ? val : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _storage.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _storage[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
}

public class FakeNotificationService : INotificationService
{
    public int ChamadasNotificacaoAssinado { get; set; }

    public Task NotificarDocumentoAssinadoAsync(int documentoId, string tituloDocumento, string nomeUsuario)
    {
        ChamadasNotificacaoAssinado++;
        return Task.CompletedTask;
    }

    public Task NotificarNovoProcessoAsync(int processoId, string numeroSequencial, string assunto)
    {
        return Task.CompletedTask;
    }
}
