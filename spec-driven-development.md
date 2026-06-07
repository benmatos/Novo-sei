# Spec-Driven Development: Guia de Tasks para Implementação do Novo SEI

Aja como um Engenheiro de Software Sênior especialista na stack Microsoft (.NET 9, Blazor Server, SQL Server). Com base nas diretrizes, arquitetura e regras de negócio definidas no arquivo `@ai-harness.md`, implemente os módulos do sistema executando estritamente as tarefas estruturadas abaixo.

---

### BLOCO 1: INFRAESTRUTURA BASE, BANCO DE DADOS E AUTENTICAÇÃO LDAP

#### Task 1.1: Modelagem de Dados e Contexto do EF Core
* Crie as entidades de domínio na camada `NovoSei.Core`: `Usuario`, `Processo`, `Documento`, `Assinatura`, `TemplateDocumento`.
* Configure o `ApplicationDbContext` na camada `NovoSei.Infrastructure` ativando `.ToTable(tb => tb.IsTemporal())` para as tabelas de `Processo` e `Documento`.
* Adicione índices de performance para `NumeroSequencial` e `Email`.

#### Task 1.2: Módulo de Autenticação LDAP e Auto-Provisionamento
* Crie o serviço `LdapAuthenticationService` utilizando `System.DirectoryServices.Protocols`.
* Implemente o método de login validando as credenciais contra o Active Directory (`connection.Bind()`).
* Caso as credenciais sejam válidas, verifique se o usuário já existe no SQL Server. Se não existir, realize o auto-provisionamento inserindo o registro do servidor com o perfil padrão "UsuarioComum".

#### Task 1.3: Middleware de Autenticação e Perfis (RBAC)
* Configure a autenticação via Cookies e estados de autenticação em cascata do Blazor no arquivo `Program.cs`.
* Mapeie o perfil do usuário do banco de dados ("Administrador", "Gestor", "UsuarioComum") como um `ClaimTypes.Role` dentro do cookie gerado no endpoint `/api/auth/login`.

---

### BLOCO 2: MOTOR DE TEMPLATES, COMPONENTES E VISUALIZAÇÃO INTEGRADA

#### Task 2.1: Motor de Substituição Dinâmica de Documentos
* Crie o serviço `TemplateEngineService` na camada `NovoSei.Core`.
* Desenvolva o método de processamento que substitui os marcadores `{{NumeroProcesso}}`, `{{DataAtual}}` e `{{TextoConteudo}}` dentro do HTML base do template pelos dados reais antes de salvar o documento.

#### Task 2.2: Componente Blazor de Visualização Integrada (Split-Pane)
* Desenvolva a página `VisualizarProcesso.razor` utilizando layout de duas colunas com Tailwind CSS (Esquerda: Árvore cronológica de documentos e trâmites; Direita: Visualizador do conteúdo do documento).
* Implemente a reatividade no Blazor para carregar e exibir o conteúdo usando `((MarkupString)conteudoHtml)` de forma instantânea ao clicar em um item da árvore, sem recarregar a página.

#### Task 2.3: Assinatura Eletrônica e Conversão para PDF
* Implemente a lógica de assinatura que bloqueia modificações no documento e gera o hash SHA-256 criptográfico.
* Integre a biblioteca `PuppeteerSharp` para ler o HTML final assinado, convertê-lo em PDF físico e salvá-lo no sistema de arquivos do Windows Server (`C:\NovoSei\Storage\`), expondo um endpoint seguro para download do arquivo.

---

### BLOCO 3: CENTRAL DE INTELIGÊNCIA (DASHBOARD OPERACIONAL)

#### Task 3.1: Queries Agregadas de Performance
* Implemente métodos de consulta usando `CountAsync` e projeções otimizadas do EF Core para consolidar os dados do usuário atual: total de processos sob responsabilidade, processos abertos, documentos rascunhos pendentes de assinatura e trâmites realizados.

#### Task 3.2: Interface de Métricas na Caixa de Entrada
* Crie o componente `IndicadoresDashboard.razor` estilizado com Tailwind CSS exibindo 4 cartões de métricas responsivos.
* Acople este componente logo no topo da página principal `CaixaEntrada.razor`, criando uma central de trabalho eficiente e de alta usabilidade para o servidor público.

---

**Instrução para a IA:** Execute as tarefas um bloco por vez. Forneça os códigos completos com as respectivas namespaces das camadas (`NovoSei.Core`, `NovoSei.Infrastructure`, `NovoSei.Web`). Aguarde o feedback de sucesso de cada bloco antes de avançar.
