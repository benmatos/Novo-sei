# Prompt: Evolução do Novo SEI para Alto Nível

## Contexto do Projeto

Você está evoluindo o **Novo SEI** — uma reescrita moderna do Sistema Eletrônico de Informações (SEI) do governo federal brasileiro — desenvolvida em **.NET 8 (C#)** com **Blazor Interactive Server**, seguindo **Clean Architecture** e **Domain-Driven Design (DDD)**.

A solução atual (`benmatos/Novo-sei`) já possui:
- 4 projetos: `NovoSei.Core`, `NovoSei.Infrastructure`, `NovoSei.Web`, `NovoSei.Tests`
- Geração automática de NUP via algoritmo Módulo 11 (Portaria Interministerial MJSP/ME nº 11/2019)
- Tabelas Temporais do SQL Server para auditoria nativa
- Autenticação com TOTP/MFA e assinaturas SHA-256
- Cache distribuído (`IDistributedCache`) com Redis
- BackgroundWorkers assíncronos
- Interface Blazor Server com SignalR e Tailwind CSS

---

## Objetivo

Implementar todas as evoluções descritas abaixo, respeitando a arquitetura existente, sem quebrar contratos de domínio já estabelecidos. Cada nível deve ser entregue de forma incremental e testável de forma independente.

---

## Nível 1 — Consolidação e Observabilidade (Curto Prazo)

### 1.1 Cobertura de Testes Críticos
No projeto `NovoSei.Tests`, implemente testes unitários e de integração cobrindo obrigatoriamente:
- Algoritmo de geração de NUP com Módulo 11 — testar sequencial, ano, unidade e dígitos verificadores com dados reais
- Serviço TOTP: geração, validação, expiração e reuso de códigos
- Motor de SLA: cálculo de prazo em dias úteis, feriados, vencimento e alertas
- Controle de acesso: regras de Público/Restrito/Sigiloso por perfil de usuário
- Auditoria temporal: garantir que inserções e alterações geram entradas nas tabelas históricas do SQL Server

Use `xUnit`, `Moq` e `FluentAssertions`. Para testes de integração com banco, use `Testcontainers` com SQL Server.

### 1.2 Observabilidade com OpenTelemetry
Em `NovoSei.Infrastructure` e `NovoSei.Web`, adicione:
- **Traces distribuídos** via `OpenTelemetry.Instrumentation.AspNetCore` e `OpenTelemetry.Instrumentation.EntityFrameworkCore`
- **Métricas** de processos criados/hora, documentos assinados, tempo médio de tramitação
- **Logs estruturados** com `Serilog` exportando para console (dev) e Seq ou Application Insights (prod)
- **Health checks** em `/health` e `/health/ready` cobrindo SQL Server, Redis e dependências externas

### 1.3 Validação de Entrada Centralizada
Substitua validações espalhadas por `FluentValidation` em todos os Commands e DTOs de entrada:
- `CriarProcessoCommand`, `TramitarProcessoCommand`, `AssinarDocumentoCommand` etc.
- Registrar validators automaticamente via `AddValidatorsFromAssembly`
- Retornar erros de validação padronizados no formato RFC 7807 (Problem Details)

### 1.4 Rate Limiting e Auditoria de Acesso
- Implementar `RateLimiter` nativo do .NET 8 em endpoints sensíveis (login, assinatura, download)
- Registrar em tabela de auditoria: usuário, IP, ação, timestamp e resultado (sucesso/falha) para cada operação privilegiada

---

## Nível 2 — Inteligência Documental (Médio Prazo)

### 2.1 OCR e Extração de Texto
Em `NovoSei.Infrastructure`, criar serviço `IDocumentOcrService`:
- Para PDFs digitais: extrair texto via `PdfPig` (já presente) ou `iText7`
- Para PDFs escaneados e imagens: integrar **Tesseract OCR** via `Tesseract` NuGet (wrapper .NET)
- Alternativa enterprise: cliente HTTP para **Azure AI Document Intelligence** (Form Recognizer)
- Armazenar texto extraído em coluna `TextoIndexado` (tipo `nvarchar(max)`) nas tabelas `Documentos`
- Processar extração de forma assíncrona via `BackgroundWorker` existente, com fila em memória ou Redis

### 2.2 Indexação Full-Text e Busca Semântica
- Ativar **Full-Text Search do SQL Server** na coluna `TextoIndexado` de `Documentos`
- Para busca semântica, implementar `ISemanticSearchService`:
  - Gerar embeddings dos documentos via **Azure OpenAI** (`text-embedding-3-small`) ou modelo local via `llama.cpp` HTTP API
  - Armazenar vetores em **pgvector** (PostgreSQL) ou como coluna `vector` no SQL Server 2025, ou usar **Azure AI Search**
  - Endpoint de busca: `POST /api/busca` com `{ query: string, filtros: { unidade, tipo, periodo } }`
  - Retornar documentos rankeados por similaridade cossenoidal

### 2.3 Extração Automática de Metadados via LLM
Criar `IMetadataExtractorService` que, ao receber o texto de um documento:
- Identifica: tipo do documento, partes envolvidas, datas relevantes, valores monetários, prazos legais
- Usa prompt estruturado para retornar JSON com os metadados extraídos
- Salva metadados em tabela `DocumentoMetadados` com chave-valor e tipo de dado
- Permite configuração do endpoint LLM (Azure OpenAI, Ollama local, ou Anthropic Claude via API)

### 2.4 Sugestão de Tipo de Processo na Autuação
No componente Blazor de autuação:
- Ao usuário digitar a especificação do processo (campo de texto livre), após 500ms de debounce:
- Chamar endpoint `POST /api/processos/sugerir-tipo` com o texto parcial
- Retornar top 3 tipos sugeridos com score de confiança
- Exibir sugestões como badges clicáveis abaixo do campo, aplicando o tipo ao clicar

---

## Nível 3 — Automação de Fluxos e Integração (Médio-Longo Prazo)

### 3.1 Motor de Workflow BPMN
Substituir tramitação manual por motor executável:
- Integrar **Elsa Workflows 3.x** (`.NET 8 nativo`) como motor de processos
- Modelar fluxos em BPMN 2.0 com atividades customizadas do SEI: `TramitarParaUnidade`, `SolicitarAssinatura`, `NotificarInteressado`, `EncerrarProcesso`
- Persistir estado do workflow no banco (`IWorkflowInstanceStore` com EF Core)
- Painel de administração de workflows acessível apenas por perfil `Administrador`

### 3.2 SLA Ativo com Notificações
Expandir o `SlaService` existente:
- Calcular prazo em dias úteis descontando feriados nacionais (consumir API do IBGE: `servicodados.ibge.gov.br/api/v1/json/calendario`)
- Criar `SlaMonitorWorker` que a cada hora verifica processos próximos do vencimento
- Enviar notificações via SignalR para usuários ativos na mesma unidade
- Registrar alertas em tabela `AlertasSla` com status (pendente/lido/ignorado)
- Exibir badge numérico de alertas no menu lateral do Blazor

### 3.3 Assinatura Digital ICP-Brasil
Criar `IAssinaturaDigitalService` com suporte a:
- **Certificados A1** (arquivo PFX): assinar hash SHA-256 do documento com chave privada via `System.Security.Cryptography`
- **Certificados A3** (token/smartcard): integrar via `PKCS#11` usando `Net.Pkcs11Interop`
- Gerar **carimbo de tempo (timestamp)** via servidor TSA do ITI (`timestamp.serpro.gov.br`) seguindo RFC 3161
- Validar cadeia de certificação ICP-Brasil contra a lista de ACs confiáveis publicada pelo ITI
- Armazenar assinatura em formato **CAdES** (`.p7s`) referenciada ao documento original

### 3.4 API REST Pública Versionada
Em `NovoSei.Web`, criar namespace `Api/v1/`:
- Endpoints RESTful para: `GET /api/v1/processos`, `GET /api/v1/processos/{nup}`, `GET /api/v1/documentos/{id}/download`
- Autenticação via **JWT Bearer Token** com escopos por operação
- Versionamento via header `api-version` usando `Asp.Versioning`
- Documentação automática via **Scalar** (substituto moderno do Swagger UI) com exemplos por endpoint
- Rate limiting por API key com quotas configuráveis por cliente

---

## Nível 4 — Plataforma Distribuída (Longo Prazo)

### 4.1 Multi-Tenant
Arquitetura para múltiplos órgãos numa única instância:
- Identificar tenant via subdomínio (`mte.novosei.gov.br`) ou header `X-Tenant-Id`
- Isolamento por **schema de banco de dados** (um schema por órgão, mesmo SQL Server)
- `TenantMiddleware` que resolve e injeta `ITenantContext` em toda a cadeia de request
- Configurações por tenant: logo, paleta de cores, tipos de processo, integração LDAP própria

### 4.2 Arquitetura Event-Driven
Substituir chamadas síncronas inter-serviços por eventos de domínio:
- Integrar **MassTransit** com **RabbitMQ** (on-premise) ou **Azure Service Bus** (cloud)
- Publicar eventos: `ProcessoCriado`, `DocumentoAssinado`, `ProcessoTramitado`, `SlaVencido`
- Consumidores independentes: serviço de notificação, serviço de indexação, serviço de auditoria
- Garantir **idempotência** nos consumidores com tabela de mensagens processadas
- Implementar **Outbox Pattern** para garantir consistência entre DB e fila

### 4.3 AI Copilot Contextual
Assistente embutido na interface Blazor:
- Painel lateral deslizante com chat contextual ao processo/documento aberto
- Capacidades: resumir processos longos, redigir minutas a partir de template + contexto, identificar inconsistências entre documentos, sugerir próxima ação com base no histórico de processos similares
- Histórico de conversa por sessão, salvo em `CopilotSessoes`
- Configurável para usar Azure OpenAI (GPT-4o) ou Anthropic Claude via API
- Nunca expor dados sigilosos ao LLM: filtrar por nível de acesso antes de montar o contexto

### 4.4 Zero Trust e Segurança Avançada
- **Autenticação contínua**: verificar contexto de risco a cada operação privilegiada (IP, dispositivo, horário, padrão de comportamento)
- **RBAC granular**: permissões por recurso + ação + unidade organizacional (não apenas por perfil global)
- **Detecção de anomalias**: alertar quando usuário acessa volume anormal de processos, tenta ações fora do horário usual ou de IP desconhecido
- **Criptografia em repouso**: documentos sigilosos criptografados com chave derivada por processo usando `AES-256-GCM`
- **Conformidade LGPD**: funcionalidade de anonimização de dados pessoais de interessados mediante decisão judicial ou prazo de retenção expirado

---

## Restrições e Padrões a Seguir em Toda a Evolução

- **Nunca quebrar a interface `IRepository<T>` existente** — extensões via novos métodos opcionais ou novos repositórios especializados
- **Migrations EF Core** para toda alteração de schema — sem scripts SQL avulsos
- **Feature Flags** via `Microsoft.FeatureManagement` para habilitar/desabilitar cada nível em produção sem redeploy
- **Backward compatibility** na API pública — versionar antes de alterar contratos existentes
- **Sem dependências circulares** entre projetos — `Core` não referencia `Infrastructure` ou `Web`
- **Localização**: todas as strings de interface em pt-BR, com suporte a `IStringLocalizer` para expansão futura
- **Acessibilidade**: componentes Blazor com atributos ARIA, contraste WCAG AA mínimo, navegação por teclado

---

## Entregáveis Esperados por Nível

Ao implementar cada nível, entregar:
1. Código-fonte completo dos novos serviços, interfaces e componentes
2. Migration EF Core correspondente (se houver alteração de schema)
3. Testes unitários/integração para a funcionalidade implementada
4. Atualização do `README.md` com instruções de configuração e variáveis de ambiente necessárias
5. Exemplo de configuração em `appsettings.Development.json` e `appsettings.Production.json`
