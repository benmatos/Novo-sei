using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Microsoft.Extensions.Caching.Distributed;


namespace NovoSei.Infrastructure.Services;

public class DocumentoService(
    ApplicationDbContext db,
    ILdapAuthenticationService ldapService,
    ITemplateEngineService templateEngine,
    IFileStorageService storageService,
    IDistributedCache cache,
    INotificationService notificationService) : IDocumentoService
{
    public async Task<Processo?> ObterProcessoComDocumentosAsync(int processoId)
    {
        return await db.Processos
            .Include(p => p.Usuario)
            .Include(p => p.Documentos)
                .ThenInclude(d => d.Assinaturas)
                    .ThenInclude(a => a.Usuario)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == processoId);
    }

    public async Task<Documento?> ObterDocumentoPorIdAsync(int documentoId)
    {
        return await db.Documentos
            .Include(d => d.Processo)
            .Include(d => d.TemplateDocumento)
            .Include(d => d.Assinaturas)
                .ThenInclude(a => a.Usuario)
            .FirstOrDefaultAsync(d => d.Id == documentoId);
    }

    public async Task<Documento> CriarDocumentoAsync(int processoId, int templateId, string titulo, string textoConteudo)
    {
        var processo = await db.Processos.FindAsync(processoId) 
            ?? throw new ArgumentException("Processo não encontrado.");

        var template = await db.TemplatesDocumento.FindAsync(templateId)
            ?? throw new ArgumentException("Template não encontrado.");

        // Obter HTML processado
        var htmlProcessado = templateEngine.ProcessarTemplate(
            template.ConteudoHtmlBase, 
            processo.NumeroSequencial, 
            textoConteudo
        );

        var documento = new Documento
        {
            ProcessoId = processoId,
            TemplateDocumentoId = templateId,
            Titulo = titulo,
            ConteudoHtml = htmlProcessado,
            Status = "Rascunho",
            CriadoEm = DateTime.UtcNow
        };

        db.Documentos.Add(documento);
        await db.SaveChangesAsync();

        try
        {
            await cache.RemoveAsync($"dashboard:usuario:{processo.UsuarioId}");
        }
        catch { }

        return documento;
    }

    public async Task<bool> AssinarDocumentoAsync(int documentoId, string login, string senha)
    {
        var documento = await db.Documentos
            .Include(d => d.Processo)
            .Include(d => d.Assinaturas)
            .FirstOrDefaultAsync(d => d.Id == documentoId)
            ?? throw new ArgumentException("Documento não encontrado.");

        // Imutabilidade Documental: se já estiver assinado, não pode modificar
        if (documento.Status == "Assinado")
            throw new InvalidOperationException("Este documento já está assinado e não pode ser modificado ou assinado novamente.");

        // Validação no LDAP
        var loginResponse = await ldapService.AutenticarAsync(login, senha);
        if (loginResponse is null)
            return false;

        var usuario = await db.Usuarios.FindAsync(loginResponse.Id)
            ?? throw new InvalidOperationException("Usuário autenticado não encontrado no banco local.");

        var timestamp = DateTime.UtcNow;

        // Geração do Hash SHA-256 nativo combinando: DocumentoId + ConteudoHtml + UsuarioId + Timestamp
        var rawInput = $"{documento.Id}{documento.ConteudoHtml}{usuario.Id}{timestamp:o}";
        var inputBytes = Encoding.UTF8.GetBytes(rawInput);
        var hashBytes = SHA256.HashData(inputBytes);
        var hashHex = Convert.ToHexString(hashBytes).ToLower();

        var assinatura = new Assinatura
        {
            DocumentoId = documento.Id,
            UsuarioId = usuario.Id,
            HashSha256 = hashHex,
            AssinadoEm = timestamp
        };

        documento.Assinaturas.Add(assinatura);
        documento.Status = "Assinado";
        documento.AtualizadoEm = timestamp;

        var pdfFileName = $"{documento.Id}_{timestamp:yyyyMMddHHmmss}.pdf";
        documento.CaminhoArquivoPdf = pdfFileName;

        // Persistir no Banco primeiro antes de gerar o PDF
        await db.SaveChangesAsync();

        // Conversão de PDF usando PuppeteerSharp
        try
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            var launchOptions = new LaunchOptions 
            { 
                Headless = true 
            };
            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();

            // Adiciona metadados de assinatura no final do HTML para exibição no PDF
            var htmlComAssinatura = $@"
                {documento.ConteudoHtml}
                <hr style='margin-top: 50px;' />
                <div style='font-family: sans-serif; font-size: 12px; color: #555;'>
                    <p><b>Documento assinado eletronicamente por:</b> {usuario.Nome} ({usuario.Email})</p>
                    <p><b>Data/Hora da Assinatura:</b> {timestamp:dd/MM/yyyy HH:mm:ss} UTC</p>
                    <p><b>Hash SHA-256:</b> {hashHex}</p>
                </div>";

            await page.SetContentAsync(htmlComAssinatura);
            
            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
            };

            var pdfData = await page.PdfDataAsync(pdfOptions);
            var storageKey = await storageService.SalvarArquivoAsync(pdfFileName, pdfData);

            documento.CaminhoArquivoPdf = storageKey;
            await db.SaveChangesAsync();

            try
            {
                await cache.RemoveAsync($"dashboard:usuario:{usuario.Id}");
                if (documento.Processo != null && documento.Processo.UsuarioId != usuario.Id)
                {
                    await cache.RemoveAsync($"dashboard:usuario:{documento.Processo.UsuarioId}");
                }

                await notificationService.NotificarDocumentoAssinadoAsync(documento.Id, documento.Titulo, usuario.Nome);
            }
            catch { }
        }
        catch (Exception ex)
        {
            // Opcional: registrar erro de PuppeteerSharp mas retornar true porque no banco foi salvo
            // No entanto, para produção, é bom deixar a exceção subir ou registrar
            throw new InvalidOperationException($"Falha ao gerar arquivo PDF com Puppeteer: {ex.Message}", ex);
        }

        return true;
    }

    public async Task<byte[]?> ObterPdfBytesAsync(int documentoId)
    {
        var documento = await db.Documentos.FindAsync(documentoId);
        if (string.IsNullOrEmpty(documento?.CaminhoArquivoPdf))
            return null;

        return await storageService.ObterArquivoAsync(documento.CaminhoArquivoPdf);
    }

    public async Task<List<ProcessoVersaoDto>> ObterHistoricoProcessoAsync(int processoId)
    {
        return await db.Processos
            .TemporalAll()
            .Where(p => p.Id == processoId)
            .OrderByDescending(p => EF.Property<DateTime>(p, "PeriodStart"))
            .Select(p => new ProcessoVersaoDto(
                p.NumeroSequencial,
                p.Assunto,
                p.Status,
                p.UsuarioId,
                db.Usuarios.Where(u => u.Id == p.UsuarioId).Select(u => u.Nome).FirstOrDefault() ?? "Sistema",
                EF.Property<DateTime>(p, "PeriodStart"),
                EF.Property<DateTime>(p, "PeriodEnd")
            ))
            .ToListAsync();
    }

    public async Task<List<DocumentoVersaoDto>> ObterHistoricoDocumentoAsync(int documentoId)
    {
        return await db.Documentos
            .TemporalAll()
            .Where(d => d.Id == documentoId)
            .OrderByDescending(d => EF.Property<DateTime>(d, "PeriodStart"))
            .Select(d => new DocumentoVersaoDto(
                d.Id,
                d.Titulo,
                d.ConteudoHtml,
                d.Status,
                d.CaminhoArquivoPdf,
                EF.Property<DateTime>(d, "PeriodStart"),
                EF.Property<DateTime>(d, "PeriodEnd"),
                d.UnidadeId,
                d.UnidadeId.HasValue ? db.Unidades.Where(u => u.Id == d.UnidadeId.Value).Select(u => u.Sigla).FirstOrDefault() : null
            ))
            .ToListAsync();
    }
}
