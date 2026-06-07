using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NovoSei.Core.Entities;
using NovoSei.Core.Interfaces;
using NovoSei.Infrastructure.Data;

namespace NovoSei.Infrastructure.Services;

public class IngestaoLegacyService(
    ApplicationDbContext db,
    IDistributedCache cache) : IIngestaoLegacyService
{
    public async Task<IngestaoStatsDto> ObterEstatisticasLegadoAsync()
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State == ConnectionState.Closed;
        if (wasClosed) await conn.OpenAsync();

        try
        {
            int totalUsuarios = 0;
            int totalProcessos = 0;
            int totalDocumentos = 0;
            int totalAssinaturas = 0;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM [sei].[dbo].[usuario] WHERE sin_ativo = 'S'";
                totalUsuarios = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM [sei].[dbo].[procedimento] p 
                    INNER JOIN [sei].[dbo].[protocolo] pr ON p.id_procedimento = pr.id_protocolo 
                    WHERE pr.sin_eliminado = 'N'";
                totalProcessos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM [sei].[dbo].[documento] d 
                    INNER JOIN [sei].[dbo].[protocolo] pr ON d.id_documento = pr.id_protocolo 
                    WHERE pr.sin_eliminado = 'N'";
                totalDocumentos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM [sei].[dbo].[assinatura] WHERE sin_ativo = 'S'";
                totalAssinaturas = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            return new IngestaoStatsDto(totalUsuarios, totalProcessos, totalDocumentos, totalAssinaturas);
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    public async Task<IngestaoResult> IngerirDadosLegadosAsync()
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State == ConnectionState.Closed;
        if (wasClosed) await conn.OpenAsync();

        var usuariosImportados = 0;
        var processosImportados = 0;
        var documentosImportados = 0;
        var assinaturasImportadas = 0;

        try
        {
            // 1. Garantir o Template Padrão para os documentos legados
            var templateLegado = await db.TemplatesDocumento.FirstOrDefaultAsync(t => t.Nome == "Template Legado Importado");
            if (templateLegado == null)
            {
                templateLegado = new TemplateDocumento
                {
                    Nome = "Template Legado Importado",
                    ConteudoHtmlBase = @"
                        <div class='legacy-document-wrapper'>
                            <h2 style='text-align:center;'>DOCUMENTO IMPORTADO DO SEI LEGADO</h2>
                            <hr />
                            <div class='legacy-content'>
                                {{TextoConteudo}}
                            </div>
                        </div>",
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                };
                db.TemplatesDocumento.Add(templateLegado);
                await db.SaveChangesAsync();
            }

            // 2. Importar Órgãos (SIP)
            var legacyOrgaos = new List<(int LegacyId, string Sigla, string Descricao, bool Ativo)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id_orgao, sigla, descricao, sin_ativo FROM [sip].[dbo].[orgao]";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        legacyOrgaos.Add((reader.GetInt32(0), reader.GetString(1).Trim(), reader.GetString(2).Trim(), reader.GetString(3) == "S"));
                    }
                }
            }

            var orgaoMap = new Dictionary<int, int>();
            foreach (var lo in legacyOrgaos)
            {
                var existing = await db.Orgaos.FirstOrDefaultAsync(o => o.Sigla == lo.Sigla);
                if (existing == null)
                {
                    existing = new Orgao { Sigla = lo.Sigla, Descricao = lo.Descricao, Ativo = lo.Ativo };
                    db.Orgaos.Add(existing);
                    await db.SaveChangesAsync();
                }
                orgaoMap[lo.LegacyId] = existing.Id;
            }

            // 3. Importar Unidades (SIP)
            var legacyUnidades = new List<(int LegacyId, int LegacyOrgaoId, string Sigla, string Descricao, bool Ativo)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id_unidade, id_orgao, sigla, descricao, sin_ativo FROM [sip].[dbo].[unidade]";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        legacyUnidades.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2).Trim(), reader.GetString(3).Trim(), reader.GetString(4) == "S"));
                    }
                }
            }

            var unidadeMap = new Dictionary<int, int>();
            foreach (var lu in legacyUnidades)
            {
                var localOrgaoId = orgaoMap.TryGetValue(lu.LegacyOrgaoId, out var oid) ? oid : orgaoMap.Values.FirstOrDefault();
                if (localOrgaoId == 0) continue;

                var existing = await db.Unidades.FirstOrDefaultAsync(u => u.Sigla == lu.Sigla && u.OrgaoId == localOrgaoId);
                if (existing == null)
                {
                    existing = new Unidade { Sigla = lu.Sigla, Descricao = lu.Descricao, Ativo = lu.Ativo, OrgaoId = localOrgaoId };
                    db.Unidades.Add(existing);
                    await db.SaveChangesAsync();
                }
                unidadeMap[lu.LegacyId] = existing.Id;
            }

            // Atualizar hierarquia de unidades (id_unidade_pai)
            var legacyHierarchy = new List<(int LegacyId, int? LegacyParentId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id_unidade, id_unidade_pai FROM [sip].[dbo].[rel_hierarquia_unidade] WHERE sin_ativo = 'S'";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var uid = reader.GetInt32(0);
                        var upid = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                        legacyHierarchy.Add((uid, upid));
                    }
                }
            }

            foreach (var rel in legacyHierarchy)
            {
                if (unidadeMap.TryGetValue(rel.LegacyId, out var localId))
                {
                    int? localParentId = null;
                    if (rel.LegacyParentId.HasValue && unidadeMap.TryGetValue(rel.LegacyParentId.Value, out var pid))
                    {
                        localParentId = pid;
                    }

                    var localUnidade = await db.Unidades.FindAsync(localId);
                    if (localUnidade != null && localParentId.HasValue && localUnidade.ParentUnidadeId != localParentId)
                    {
                        localUnidade.ParentUnidadeId = localParentId;
                        db.Unidades.Update(localUnidade);
                    }
                }
            }
            await db.SaveChangesAsync();

            // 4. Carregar Usuários Legados
            var legacyUsers = new List<(int LegacyId, string Sigla, string Nome, string Email)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT u.id_usuario, u.sigla, u.nome, c.email 
                    FROM [sei].[dbo].[usuario] u 
                    LEFT JOIN [sei].[dbo].[contato] c ON u.id_contato = c.id_contato
                    WHERE u.sin_ativo = 'S'";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var legacyId = reader.GetInt32(0);
                        var sigla = reader.GetString(1).Trim();
                        var nome = reader.GetString(2).Trim();
                        var email = reader.IsDBNull(3) ? $"{sigla}@novosei.gov.br" : reader.GetString(3).Trim();

                        legacyUsers.Add((legacyId, sigla, nome, email));
                    }
                }
            }

            var userMap = new Dictionary<int, int>();
            var localUsers = await db.Usuarios.ToListAsync();
            var localUserMapByLogin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var lu in localUsers)
            {
                localUserMapByLogin[lu.Login] = lu.Id;
            }

            foreach (var lu in legacyUsers)
            {
                if (localUserMapByLogin.TryGetValue(lu.Sigla, out var localId))
                {
                    userMap[lu.LegacyId] = localId;
                }
                else
                {
                    var emailFinal = lu.Email;
                    int suffix = 1;
                    while (await db.Usuarios.AnyAsync(u => u.Email == emailFinal))
                    {
                        var parts = lu.Email.Split('@');
                        emailFinal = $"{parts[0]}{suffix}@{parts[1]}";
                        suffix++;
                    }

                    var novoUser = new Usuario
                    {
                        Login = lu.Sigla,
                        Nome = lu.Nome,
                        Email = emailFinal,
                        Perfil = "UsuarioComum",
                        CriadoEm = DateTime.UtcNow
                    };
                    db.Usuarios.Add(novoUser);
                    await db.SaveChangesAsync();

                    userMap[lu.LegacyId] = novoUser.Id;
                    localUserMapByLogin[novoUser.Login] = novoUser.Id;
                    usuariosImportados++;
                }
            }

            var defaultUserId = 1;
            var defaultUser = await db.Usuarios.FirstOrDefaultAsync();
            if (defaultUser != null)
            {
                defaultUserId = defaultUser.Id;
            }

            // Vincular usuários às suas respectivas unidades (many-to-many em permissao)
            var userPermissions = new List<(int LegacyUserId, int LegacyUnidadeId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT id_usuario, id_unidade FROM [sip].[dbo].[permissao] WHERE id_sistema = 100000100";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        userPermissions.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }
            }

            foreach (var perm in userPermissions)
            {
                if (userMap.TryGetValue(perm.LegacyUserId, out var localUserId) &&
                    unidadeMap.TryGetValue(perm.LegacyUnidadeId, out var localUnidadeId))
                {
                    var user = await db.Usuarios.Include(u => u.Unidades).FirstOrDefaultAsync(u => u.Id == localUserId);
                    var unit = await db.Unidades.FindAsync(localUnidadeId);
                    if (user != null && unit != null && !user.Unidades.Any(u => u.Id == localUnidadeId))
                    {
                        user.Unidades.Add(unit);
                    }
                }
            }
            await db.SaveChangesAsync();

            // 5. Carregar Processos Legados (com UnidadeId)
            var legacyProcs = new List<(long LegacyId, string Numero, string Descricao, DateTime CriadoEm, int LegacyUserGerador, int LegacyUnidadeGeradora)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT p.id_procedimento, pr.protocolo_formatado, pr.descricao, pr.dta_geracao, pr.id_usuario_gerador, pr.id_unidade_geradora
                    FROM [sei].[dbo].[procedimento] p
                    INNER JOIN [sei].[dbo].[protocolo] pr ON p.id_procedimento = pr.id_protocolo
                    WHERE pr.sin_eliminado = 'N'";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var legacyId = reader.GetInt64(0);
                        var numero = reader.GetString(1).Trim();
                        var descricao = reader.IsDBNull(2) ? "Importado do SEI Legado" : reader.GetString(2).Trim();
                        if (string.IsNullOrEmpty(descricao)) descricao = "Importado do SEI Legado";
                        var criadoEm = reader.GetDateTime(3);
                        var legacyUserGerador = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                        var legacyUnidadeGeradora = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);

                        legacyProcs.Add((legacyId, numero, descricao, criadoEm, legacyUserGerador, legacyUnidadeGeradora));
                    }
                }
            }

            var processMap = new Dictionary<long, int>();
            foreach (var lp in legacyProcs)
            {
                var localUserId = userMap.TryGetValue(lp.LegacyUserGerador, out var uid) ? uid : defaultUserId;
                var localUnidadeId = unidadeMap.TryGetValue(lp.LegacyUnidadeGeradora, out var unId) ? (int?)unId : null;

                var pLoc = await db.Processos.FirstOrDefaultAsync(p => p.NumeroSequencial == lp.Numero);
                if (pLoc == null)
                {
                    var novoProcesso = new Processo
                    {
                        NumeroSequencial = lp.Numero,
                        Assunto = lp.Descricao.Length > 500 ? lp.Descricao[..497] + "..." : lp.Descricao,
                        Status = "Aberto",
                        CriadoEm = lp.CriadoEm,
                        UsuarioId = localUserId,
                        UnidadeId = localUnidadeId
                    };
                    db.Processos.Add(novoProcesso);
                    await db.SaveChangesAsync();

                    processMap[lp.LegacyId] = novoProcesso.Id;
                    processosImportados++;
                }
                else
                {
                    processMap[lp.LegacyId] = pLoc.Id;
                }
            }

            // 6. Carregar Documentos Legados (com UnidadeId)
            var legacyDocs = new List<(long LegacyId, long LegacyProcId, string NomeArvore, string SinBloqueado, string NumFormatado, string Descricao, DateTime CriadoEm, string Conteudo, int LegacyUnidadeResponsavel)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.id_documento, d.id_procedimento, d.nome_arvore, d.sin_bloqueado, 
                           pr.protocolo_formatado, pr.descricao, pr.dta_geracao, dc.conteudo, d.id_unidade_responsavel
                     FROM [sei].[dbo].[documento] d
                     INNER JOIN [sei].[dbo].[protocolo] pr ON d.id_documento = pr.id_protocolo
                     LEFT JOIN [sei].[dbo].[documento_conteudo] dc ON d.id_documento = dc.id_documento
                     WHERE pr.sin_eliminado = 'N'";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var legacyId = reader.GetInt64(0);
                        var legacyProcId = reader.GetInt64(1);
                        var nomeArvore = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim();
                        var sinBloqueado = reader.GetString(3).Trim();
                        var numFormatado = reader.GetString(4).Trim();
                        var descricao = reader.IsDBNull(5) ? string.Empty : reader.GetString(5).Trim();
                        var criadoEm = reader.GetDateTime(6);
                        var conteudo = reader.IsDBNull(7) ? "<p>Sem conteúdo cadastrado no SEI legado.</p>" : reader.GetString(7);
                        var legacyUnidadeResponsavel = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);

                        legacyDocs.Add((legacyId, legacyProcId, nomeArvore, sinBloqueado, numFormatado, descricao, criadoEm, conteudo, legacyUnidadeResponsavel));
                    }
                }
            }

            var docMap = new Dictionary<long, int>();
            foreach (var ld in legacyDocs)
            {
                if (!processMap.TryGetValue(ld.LegacyProcId, out var localProcId))
                {
                    continue;
                }

                var titulo = string.IsNullOrEmpty(ld.NomeArvore) ? ld.NumFormatado : ld.NomeArvore;
                if (string.IsNullOrEmpty(titulo)) titulo = "Documento Importado";
                var tituloFinal = titulo.Length > 300 ? titulo[..297] + "..." : titulo;
                var localUnidadeId = unidadeMap.TryGetValue(ld.LegacyUnidadeResponsavel, out var unId) ? (int?)unId : null;

                var dLoc = await db.Documentos.FirstOrDefaultAsync(d => d.ProcessoId == localProcId && d.Titulo == tituloFinal);
                if (dLoc == null)
                {
                    var status = ld.SinBloqueado == "S" ? "Assinado" : "Rascunho";
                    var novoDoc = new Documento
                    {
                        ProcessoId = localProcId,
                        TemplateDocumentoId = templateLegado.Id,
                        Titulo = tituloFinal,
                        ConteudoHtml = ld.Conteudo,
                        Status = status,
                        CriadoEm = ld.CriadoEm,
                        UnidadeId = localUnidadeId
                    };
                    db.Documentos.Add(novoDoc);
                    await db.SaveChangesAsync();

                    docMap[ld.LegacyId] = novoDoc.Id;
                    documentosImportados++;
                }
                else
                {
                    docMap[ld.LegacyId] = dLoc.Id;
                }
            }

            // 7. Carregar Assinaturas Legadas
            var legacySigs = new List<(long LegacyDocId, int LegacyUserId, string NomeSignatario, DateTime DataAssinatura)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT a.id_documento, a.id_usuario, a.nome, at.dth_abertura
                    FROM [sei].[dbo].[assinatura] a
                    LEFT JOIN [sei].[dbo].[atividade] at ON a.id_atividade = at.id_atividade
                    WHERE a.sin_ativo = 'S'";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var legacyDocId = reader.GetInt64(0);
                        var legacyUserId = reader.GetInt32(1);
                        var nomeSignatario = reader.IsDBNull(2) ? "Servidor Legado" : reader.GetString(2).Trim();
                        var dataAssinatura = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);

                        legacySigs.Add((legacyDocId, legacyUserId, nomeSignatario, dataAssinatura));
                    }
                }
            }

            foreach (var ls in legacySigs)
            {
                if (!docMap.TryGetValue(ls.LegacyDocId, out var localDocId))
                {
                    continue;
                }

                var localUserId = userMap.TryGetValue(ls.LegacyUserId, out var uid) ? uid : defaultUserId;
                var existeAss = await db.Assinaturas.AnyAsync(a => a.DocumentoId == localDocId && a.UsuarioId == localUserId);
                if (!existeAss)
                {
                    var rawInput = $"{localDocId}{ls.NomeSignatario}{localUserId}{ls.DataAssinatura:o}";
                    var inputBytes = Encoding.UTF8.GetBytes(rawInput);
                    var hashBytes = SHA256.HashData(inputBytes);
                    var hashHex = Convert.ToHexString(hashBytes).ToLower();

                    var novaAss = new Assinatura
                    {
                        DocumentoId = localDocId,
                        UsuarioId = localUserId,
                        HashSha256 = hashHex,
                        AssinadoEm = ls.DataAssinatura
                    };
                    db.Assinaturas.Add(novaAss);
                    await db.SaveChangesAsync();

                    assinaturasImportadas++;
                }
            }

            // Invalida cache do dashboard
            try
            {
                foreach (var uid in userMap.Values)
                {
                    await cache.RemoveAsync($"dashboard:usuario:{uid}");
                }
            }
            catch { }

            return new IngestaoResult(
                usuariosImportados,
                processosImportados,
                documentosImportados,
                assinaturasImportadas,
                true,
                "Ingestão de dados finalizada com sucesso."
            );
        }
        catch (Exception ex)
        {
            return new IngestaoResult(
                usuariosImportados,
                processosImportados,
                documentosImportados,
                assinaturasImportadas,
                false,
                $"Erro durante a ingestão de dados: {ex.Message}"
            );
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    public async Task LimparDadosLocaisAsync()
    {
        // 1. Deletar assinaturas
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Assinaturas]");

        // 2. Deletar documentos
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Documentos]");

        // 3. Deletar processos
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Processos]");

        // 4. Deletar mapeamento UsuarioUnidade
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [UsuariosUnidades]");

        // 5. Deletar unidades
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Unidades]");

        // 6. Deletar órgãos
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Orgaos]");

        // 7. Deletar usuários (exceto o admin/sistema local para não quebrar a sessão corrente)
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [Usuarios] WHERE [Login] <> 'admin'");

        await db.SaveChangesAsync();
    }
}
