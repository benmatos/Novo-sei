using System;
using System.IO;

namespace NovoSei.Core.Services;

public static class FileSecurityValidator
{
    public static void ValidarArquivo(string nomeArquivo, byte[] conteudo)
    {
        if (conteudo == null || conteudo.Length == 0)
        {
            throw new ArgumentException("O conteúdo do arquivo não pode ser nulo ou vazio.");
        }

        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();

        // 1. Bloquear extensões perigosas conhecidas
        var extensoesProibidas = new[] { ".exe", ".dll", ".bat", ".cmd", ".sh", ".js", ".vbs", ".msi", ".com", ".phtml", ".php", ".asp", ".aspx" };
        if (Array.Exists(extensoesProibidas, ext => ext == extensao))
        {
            throw new InvalidOperationException($"Extensão de arquivo proibida: {extensao}");
        }

        // 2. Bloquear cabeçalho executável PE (MZ) no início do arquivo (mesmo com outra extensão)
        if (conteudo.Length >= 2 && conteudo[0] == 0x4D && conteudo[1] == 0x5A)
        {
            throw new InvalidOperationException("Assinatura de arquivo executável (MZ) detectada. Upload rejeitado por motivos de segurança.");
        }

        // 3. Validação baseada em Magic Numbers (MIME Sniffing)
        if (extensao == ".pdf")
        {
            // PDF deve começar com %PDF (25 50 44 46)
            if (conteudo.Length < 4 || 
                conteudo[0] != 0x25 || 
                conteudo[1] != 0x50 || 
                conteudo[2] != 0x44 || 
                conteudo[3] != 0x46)
            {
                throw new InvalidOperationException("O arquivo .pdf não possui uma assinatura de cabeçalho PDF válida.");
            }
        }
        else if (extensao == ".png")
        {
            // PNG deve começar com 89 50 4E 47 0D 0A 1A 0A
            if (conteudo.Length < 8 || 
                conteudo[0] != 0x89 || 
                conteudo[1] != 0x50 || 
                conteudo[2] != 0x4E || 
                conteudo[3] != 0x47 || 
                conteudo[4] != 0x0D || 
                conteudo[5] != 0x0A || 
                conteudo[6] != 0x1A || 
                conteudo[7] != 0x0A)
            {
                throw new InvalidOperationException("O arquivo .png não possui uma assinatura de cabeçalho PNG válida.");
            }
        }
        else if (extensao == ".jpg" || extensao == ".jpeg")
        {
            // JPEG deve começar com FF D8 FF
            if (conteudo.Length < 3 || 
                conteudo[0] != 0xFF || 
                conteudo[1] != 0xD8 || 
                conteudo[2] != 0xFF)
            {
                throw new InvalidOperationException("O arquivo JPEG não possui uma assinatura de cabeçalho JPEG válida.");
            }
        }
    }
}
