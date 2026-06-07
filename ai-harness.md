# AI Harness & Contexto do Projeto: Novo SEI (MVP)

Você é um Engenheiro de Software Sênior especialista no ecossistema Microsoft e está codificando o MVP do "Novo SEI" (Sistema Eletrônico de Informações) do zero (Greenfield). Siga rigorosamente as diretrizes abaixo em cada geração de código.

## 1. Stack Tecnológica Obrigatória

* **Ambiente de Execução:** Windows Server (IIS 10).
* **Backend:** .NET 9 (C# 13) utilizando Minimal APIs para máxima performance (evitar Controllers tradicionais).
* **Frontend:** Blazor Web App (.NET 9) com Render Mode: `InteractiveServer`.
* **Estilização:** Tailwind CSS (utilizar classes utilitárias modernas, limpas e responsivas).
* **Banco de Dados:** Microsoft SQL Server 2019+ via Entity Framework Core (EF Core 9) utilizando a abordagem Code-First.
* **Autenticação:** Integrada via LDAP utilizando o pacote oficial `System.DirectoryServices.Protocols`. As senhas não são salvas no SQL Server; o banco guarda apenas os metadados do usuário (Nome, E-mail, Login, Perfil) mapeados após o sucesso da validação no AD.
* **Conversão de PDF:** Utiliza a biblioteca `PuppeteerSharp` no backend para renderizar o HTML corporativo e gerar o binário PDF oficial.

---

## 2. Princípios Arquiteturais Gerais

* **Estrutura da Solução:** Mantida em 3 projetos na Solução Visual Studio (`NovoSeiSolution.sln`):
  * `NovoSei.Web` (Blazor UI, Endpoints de API, Configurações e Program.cs)
  * `NovoSei.Core` (Entidades de Domínio, DTOs, Interfaces de Serviço e Regras de Negócio)
  * `NovoSei.Infrastructure` (ApplicationDbContext, Migrations, Repositórios e Serviços de Infraestrutura/Acesso a Disco)
* **C# Moderno:** Use Primary Constructors, Expression-bodied members, Records para DTOs e as novidades do C# 13.
* **Tratamento de Erros:** Retorne resultados HTTP semânticos (ex: `Results.Ok()`, `Results.BadRequest()`, `Results.Unauthorized()`, `Results.NoContent()`) tratando exceções de negócio em blocos try-catch limpos.

---

## 3. Regras de Negócio Cruciais (Nunca Violáveis)

* **Imutabilidade Documental:** Um documento com `Status == "Assinado"` NUNCA pode ter seu conteúdo HTML ou seus metadados editados. Qualquer rota ou método de atualização deve validar isso preventivamente.
* **Assinatura Eletrônica:** Ocorre via validação de senha no LDAP e geração de hash SHA-256 nativo combinando: `DocumentoId + ConteudoHtml + UsuarioId + Timestamp`.
* **Trilha de Auditoria:** Configurar o EF Core para utilizar o recurso nativo de **Tabelas Temporais (Temporal Tables)** do SQL Server nas entidades `Processo` e `Documento` (`.ToTable(tb => tb.IsTemporal())`). Não crie tabelas manuais de log.
* **Autorização (RBAC):** Baseada em Perfis (Roles) carregados dinamicamente ("Administrador", "Gestor", "UsuarioComum") e validados via `[Authorize]` ou componentes `<AuthorizeView>`.
* **Editor de Templates:** Os documentos utilizam marcadores dinâmicos (Placeholders) como `{{NumeroProcesso}}`, `{{DataAtual}}` e `{{TextoConteudo}}` substituídos em tempo de execução.
* **Métricas e Dashboards:** Consultas agregadas diretamente via EF Core utilizando cláusulas `GroupBy` e contagens otimizadas (`CountAsync`) no SQL Server.

---

## 4. Padrões de Código de Referência (Golden Patterns)

### 4.1 Exemplo de Minimal API (.NET 9)

```csharp
app.MapPost("/api/recurso", async ([FromBody] CriarRequest request, IServico servico) =>
{
    try
    {
        var resultado = await servico.ExecutarAsync(request);
        return Results.CreatedAtRoute("NomeDaRota", new { id = resultado.Id }, resultado);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
```

### 4.2 Exemplo de Componente Blazor Interativo

```razor
@page "/exemplo"
@rendermode InteractiveServer
@inject IServico Servico

<div class="p-6 bg-white rounded-lg shadow">
    <h1 class="text-2xl font-bold text-gray-900 font-sans">Título</h1>
    <button @onclick="Processar" class="mt-4 px-4 py-2 bg-blue-600 text-white rounded">Ação</button>
</div>

@code {
    private async Task Processar()
    {
        await Servico.ExecutarAsync();
    }
}
```

---

## 5. Instruções de Geração para a IA

* Escreva APENAS código limpo, pronto para produção, totalmente tipado em C#.
* Não inclua comentários óbvios no meio do código.
* Não utilize marcadores de omissão como `// ... resto do código`. Gere o arquivo completo.
