namespace NovoSei.Core.DTOs;

public record Verify2FaRequest(string Login, string Senha, string Codigo, bool ConfiarDispositivo);
