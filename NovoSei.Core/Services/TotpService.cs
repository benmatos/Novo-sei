using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NovoSei.Core.Interfaces;

namespace NovoSei.Core.Services;

public class TotpService : ITotpService
{
    private static readonly string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GerarSegredoBase32()
    {
        byte[] bytes = new byte[10];
        RandomNumberGenerator.Fill(bytes);
        
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(Base32Alphabet[b % 32]);
        }
        return sb.ToString();
    }

    public string GerarQrCodeUri(string email, string segredo)
    {
        var escapedEmail = Uri.EscapeDataString(email);
        var escapedIssuer = Uri.EscapeDataString("NovoSEI");
        return $"otpauth://totp/{escapedIssuer}:{escapedEmail}?secret={segredo}&issuer={escapedIssuer}";
    }

    public string GerarCodigoAtual(string segredoBase32)
    {
        var key = DecodificarBase32(segredoBase32);
        long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long currentStep = unixTime / 30;
        return GerarCodigoTotp(key, currentStep);
    }

    public bool ValidarCodigo(string segredoBase32, string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6 || !int.TryParse(codigo, out _))
            return false;

        byte[] key;
        try
        {
            key = DecodificarBase32(segredoBase32);
        }
        catch
        {
            return false;
        }

        long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long currentStep = unixTime / 30;

        // Validar com tolerância de ±1 passo (janela de 30 segundos antes/depois)
        for (long stepOffset = -1; stepOffset <= 1; stepOffset++)
        {
            long targetStep = currentStep + stepOffset;
            string targetCode = GerarCodigoTotp(key, targetStep);
            if (targetCode == codigo)
            {
                return true;
            }
        }

        return false;
    }

    private string GerarCodigoTotp(byte[] key, long step)
    {
        byte[] stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(stepBytes);
        }

        using var hmac = new HMACSHA1(key);
        byte[] hash = hmac.ComputeHash(stepBytes);

        int offset = hash[^1] & 0x0F;
        int binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

        int otp = binaryCode % 1_000_000;
        return otp.ToString("D6");
    }

    private byte[] DecodificarBase32(string input)
    {
        input = input.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(input)) return [];

        List<byte> bytes = new();
        int byteAccumulator = 0;
        int bitsAccumulated = 0;

        foreach (char c in input)
        {
            int value = Base32Alphabet.IndexOf(c);
            if (value < 0) continue; // Ignora caracteres inválidos

            byteAccumulator = (byteAccumulator << 5) | value;
            bitsAccumulated += 5;

            if (bitsAccumulated >= 8)
            {
                bytes.Add((byte)((byteAccumulator >> (bitsAccumulated - 8)) & 0xFF));
                bitsAccumulated -= 8;
            }
        }

        return bytes.ToArray();
    }
}
