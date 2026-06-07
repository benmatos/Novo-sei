# Prompt: Evolução do Novo SEI para Plataforma de Alto Nível

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

## Posicionamento Estratégico

O Novo SEI deve evoluir de uma reimplementação moderna do SEI tradicional para uma **plataforma soberana de gestão processual e documental**, estruturada em cinco pilares:

1. **Processo Eletrônico** — Tramitação, assinatura, workflow
2. **Gestão Documental Arquivística** — Classificação, temporalidade, preservação, acervo
3. **Interoperabilidade Governamental** — PEN, Tramita GOV, WSSEI, federação de identidade
4. **Inteligência Operacional** — Analytics, Process Mining, dashboards, Data Lake
5. **IA Aplicada** — OCR, busca semântica, extração de metadados, Copilot contextual

---

## Objetivo

Implementar todas as evoluções descritas abaixo, respeitando a arquitetura existente, sem quebrar contratos de domínio já estabelecidos. Cada nível deve ser entregue de forma incremental e testável de forma independente.

**MVP recomendado para primeira entrega:** Nível 1 + Nível 2 + Nível 3.5.1 (PEN) + Nível 4.5.3 (Dashboard básico).

---

## Nível 1 — Consolidação e Observabilidade

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

## Nível 2 — Inteligência Documental

### 2.1 OCR e Extração de Texto
Em `NovoSei.Infrastructure`, criar serviço `IDocumentOcrService`:
- Para PDFs digitais: extrair texto via `PdfPig` ou `iText7`
- Para PDFs escaneados e imagens: integrar **Tesseract OCR** via `Tesseract` NuGet (wrapper .NET)
- Alternativa enterprise: cliente HTTP para **Azure AI Document Intelligence** (Form Recognizer)
- Armazenar texto extraído em coluna `TextoIndexado` (`nvarchar(max)`) nas tabelas `Documentos`
- Processar extração de forma assíncrona via `BackgroundWorker` existente, com fila em Redis

### 2.2 Indexação Full-Text e Busca Semântica
- Ativar **Full-Text Search do SQL Server** na coluna `TextoIndexado` de `Documentos`
- Implementar `ISemanticSearchService`:
  - Gerar embeddings via **Azure OpenAI** (`text-embedding-3-small`) ou modelo local via `llama.cpp` HTTP API
  - Armazenar vetores em **pgvector** (PostgreSQL), coluna `vector` (SQL Server 2025), ou **Azure AI Search**
  - Endpoint: `POST /api/busca` com `{ query: string, filtros: { unidade, tipo, periodo } }`
  - Retornar documentos rankeados por similaridade cossenoidal

### 2.3 Extração Automática de Metadados via LLM
Criar `IMetadataExtractorService` que, ao receber o texto de um documento:
- Identifica: tipo, partes envolvidas, datas relevantes, valores monetários, prazos legais
- Usa prompt estruturado para retornar JSON com metadados extraídos
- Salva em tabela `DocumentoMetadados` com chave-valor e tipo de dado
- Configurável para Azure OpenAI, Ollama local ou Anthropic Claude via API

### 2.4 Sugestão de Tipo de Processo na Autuação
No componente Blazor de autuação:
- Ao digitar a especificação, após 500ms de debounce, chamar `POST /api/processos/sugerir-tipo`
- Retornar top 3 tipos sugeridos com score de confiança
- Exibir como badges clicáveis abaixo do campo

---

## Nível 2.5 — Gestão Documental Arquivística

Objetivo: transformar o Novo SEI em plataforma completa de gestão documental, alinhada às diretrizes do Arquivo Nacional, ao e-ARQ Brasil e ao Decreto 10.278/2020.

### 2.5.1 Plano de Classificação Documental
Implementar entidade `PlanoClassificacao` em `NovoSei.Core`:
```csharp
public class PlanoClassificacao
{
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public int Nivel { get; private set; }
    public string? CodigoPai { get; private set; }
    public bool Ativo { get; private set; }
    public int Versao { get; private set; }
}
```
- Hierarquia ilimitada de classes documentais
- Versionamento com histórico de alterações
- Associação automática de processos a classes ao criar/autuar
- Componente Blazor de navegação em árvore com busca por código e descrição
- Importação em lote via CSV/JSON conforme padrão do CONARQ

### 2.5.2 Tabela de Temporalidade e Destinação
Criar `TabelaTemporalidade` associada a cada `PlanoClassificacao`:
```csharp
public class TabelaTemporalidade
{
    public string ClasseDocumental { get; private set; }
    public int PrazoCorrenteAnos { get; private set; }
    public int PrazoIntermediarioAnos { get; private set; }
    public DestinacaoFinal DestinacaoFinal { get; private set; } // GuardaPermanente | Eliminacao | Recolhimento
    public string Fundamento { get; private set; }
}
```
- Cálculo automático da data de destinação com base na data de encerramento do processo
- `TemporalidadeMonitorWorker` que alerta vencimentos com antecedência configurável (ex: 90 dias)
- Bloqueio de eliminação indevida: processo só pode ser eliminado após aprovação em `ListaEliminacao` gerada automaticamente (conforme Resolução CONARQ nº 40/2014)
- Publicar evento `PrazoTemporalidadeVencido` para consumo pelo módulo analítico

### 2.5.3 Preservação Digital
Criar `IPreservacaoDigitalService` em `NovoSei.Infrastructure`:
- Verificação periódica de integridade via recomputação de hash SHA-256 e comparação com hash original
- Revalidação de assinaturas ICP-Brasil contra estado atual da cadeia de certificação
- Migração controlada de formatos: PDF/A-1b → PDF/A-2b ou PDF/A-3b com registro de migração
- Seguir recomendações do modelo OAIS (ISO 14721) para pacotes de submissão (SIP), arquivamento (AIP) e disseminação (DIP)
- Publicar eventos de domínio: `DocumentoPreservado`, `DocumentoCorrompido`, `DocumentoMigrado`
- Registrar todos os eventos em `LogPreservacao` com data, tipo, resultado e agente responsável

### 2.5.4 Gestão de Acervo e Ciclo de Vida
Implementar as quatro operações arquivísticas fundamentais:
- **Transferência**: envio da fase corrente para intermediária, com geração de `TermoTransferencia`
- **Recolhimento**: envio ao arquivo permanente com `GuiaRecolhimento` conforme padrão do AN
- **Eliminação**: execução apenas após `ListaEliminacao` aprovada e publicada no DOU (simulado ou real)
- **Custódia**: registro do responsável pela guarda em cada fase

Entidades: `Acervo`, `LoteDocumental`, `EventoArquivistico`
Criar `NovoSei.Arquivo` como projeto separado referenciando apenas `NovoSei.Core`

---

## Nível 3 — Automação de Fluxos e Integração

### 3.1 Motor de Workflow BPMN
- Integrar **Elsa Workflows 3.x** como motor de processos nativo .NET 8
- Modelar fluxos em BPMN 2.0 com atividades customizadas: `TramitarParaUnidade`, `SolicitarAssinatura`, `NotificarInteressado`, `EncerrarProcesso`, `AguardarAssinatura`, `VerificarTemporalidade`
- Persistir estado do workflow no banco via `IWorkflowInstanceStore` com EF Core
- Painel de administração de workflows acessível apenas por perfil `Administrador`

### 3.2 SLA Ativo com Notificações
- Calcular prazo em dias úteis descontando feriados nacionais via API do IBGE (`servicodados.ibge.gov.br/api/v1/json/calendario`)
- `SlaMonitorWorker` a cada hora verificando processos próximos do vencimento
- Notificações via SignalR para usuários ativos na mesma unidade
- Tabela `AlertasSla` com status (pendente/lido/ignorado)
- Badge numérico de alertas no menu lateral do Blazor

### 3.3 Assinatura Digital ICP-Brasil
Criar `IAssinaturaDigitalService`:
- **Certificados A1** (PFX): assinar hash SHA-256 via `System.Security.Cryptography`
- **Certificados A3** (token/smartcard): integrar via `PKCS#11` usando `Net.Pkcs11Interop`
- **Carimbo de tempo (timestamp)** via TSA do ITI (`timestamp.serpro.gov.br`) seguindo RFC 3161
- Validar cadeia ICP-Brasil contra lista de ACs do ITI
- Armazenar assinatura em formato **CAdES** (`.p7s`)

### 3.4 API REST Pública Versionada
Em `NovoSei.Web`, criar namespace `Api/v1/`:
- Endpoints: `GET /api/v1/processos`, `GET /api/v1/processos/{nup}`, `GET /api/v1/documentos/{id}/download`
- Autenticação via **JWT Bearer Token** com escopos por operação
- Versionamento via header `api-version` usando `Asp.Versioning`
- Documentação via **Scalar** com exemplos por endpoint
- Rate limiting por API key com quotas configuráveis

---

## Nível 3.5 — Interoperabilidade Governamental

Objetivo: integração nativa com o ecossistema do Processo Eletrônico Nacional (PEN), garantindo que o Novo SEI seja adotável por qualquer órgão federal.

### 3.5.1 Integração PEN (Processo Eletrônico Nacional)
Criar projeto `NovoSei.PEN` referenciando `NovoSei.Core`:
```csharp
public interface IPenClient
{
    Task<ProtocoloPen> EnviarProcessoAsync(EnvioProcessoPenDto dto);
    Task<ProcessoPen> ReceberProcessoAsync(string protocolo);
    Task<StatusPen> ConsultarStatusAsync(string protocolo);
    Task SincronizarStatusAsync();
}

public interface IPenProcessoService
{
    Task ProcessarRecebimentoAsync(ProcessoPen processo);
    Task ConfirmarRecebimentoAsync(string protocolo);
    Task RejeitarProcessoAsync(string protocolo, string motivo);
}
```
- Autenticação mútua TLS com certificado do órgão junto ao Ministério da Gestão
- `PenSyncWorker` que a cada 5 minutos verifica processos pendentes de recebimento
- Mapeamento automático de tipos de processo entre nomenclatura local e tabela PEN
- Histórico completo de envios/recebimentos em `HistoricoPen`

### 3.5.2 Integração Tramita GOV.BR
Integrar com a plataforma Tramita GOV.BR para tramitação federada:
```csharp
public class TramitacaoFederada
{
    public Guid Id { get; private set; }
    public string NupOrigem { get; private set; }
    public string SiglaSistemaOrigem { get; private set; }
    public string CodOrgaoDestino { get; private set; }
    public string SiglaUnidadeDestino { get; private set; }
    public DateTime DataEnvio { get; private set; }
    public DateTime? DataRecebimento { get; private set; }
    public StatusTramitacaoFederada Status { get; private set; }
    public string? MotivoRejeicao { get; private set; }
}
```
- Envio entre órgãos com confirmação automática de recebimento
- Webhook para recebimento de notificações do Tramita GOV.BR
- Histórico de tramitação federada visível na timeline do processo
- Geração automática do `ComponenteDigital` no formato exigido pelo protocolo

### 3.5.3 Camada de Compatibilidade WSSEI
Criar `IWsSeiCompatibilityLayer` expondo endpoints equivalentes ao mod-wssei original:
- `GET /wssei/v2/processos`, `POST /wssei/v2/processos/iniciar`, etc.
- Tradução de contratos legados para o modelo de domínio do Novo SEI
- Configurável por feature flag: habilitar apenas durante período de migração
- Documentar mapeamento entre operações WSSEI legadas e endpoints v1 nativos

### 3.5.4 Federação de Identidade
Criar `IIdentityProvider` com implementações plugáveis:
```csharp
public interface IIdentityProvider
{
    Task<UsuarioAutenticado?> AutenticarAsync(CredenciaisDto credenciais);
    Task<IEnumerable<string>> ObterPermissoesAsync(string usuarioId);
    Task<bool> ValidarTokenAsync(string token);
}

// Implementações:
public class LdapIdentityProvider : IIdentityProvider { }
public class ActiveDirectoryProvider : IIdentityProvider { }
public class OidcIdentityProvider : IIdentityProvider { }
public class GovBrIdentityProvider : IIdentityProvider { }
```
- **Gov.br**: integração via OAuth 2.0/OIDC com os níveis de confiança Bronze, Prata e Ouro
- **LDAP/AD**: suporte a múltiplos servidores com fallback
- **OIDC genérico**: compatível com Keycloak, Azure AD, Okta
- Seleção do provider por tenant (multi-tenant) ou por configuração global

---

## Nível 4 — Plataforma Distribuída

### 4.1 Multi-Tenant
- Identificar tenant via subdomínio (`mte.novosei.gov.br`) ou header `X-Tenant-Id`
- Isolamento por schema de banco de dados (um schema por órgão, mesmo SQL Server)
- `TenantMiddleware` injetando `ITenantContext` em toda a cadeia de request
- Configurações por tenant: logo, paleta, tipos de processo, integração LDAP própria

### 4.2 Arquitetura Event-Driven
- Integrar **MassTransit** com **RabbitMQ** (on-premise) ou **Azure Service Bus** (cloud)
- Eventos de domínio: `ProcessoCriado`, `DocumentoAssinado`, `ProcessoTramitado`, `SlaVencido`, `PrazoTemporalidadeVencido`, `ProcessoEnviadoPen`, `ProcessoRecebidoPen`
- Consumidores independentes: notificação, indexação, auditoria, analytics, preservação
- Idempotência nos consumidores com tabela de mensagens processadas
- **Outbox Pattern** para consistência entre DB e fila

### 4.3 AI Copilot Contextual
Assistente embutido no Blazor:
- Painel lateral deslizante contextual ao processo/documento aberto
- Capacidades: resumir processos, redigir minutas, identificar inconsistências, sugerir próxima ação, responder dúvidas sobre temporalidade e classificação documental
- Histórico de conversa por sessão em `CopilotSessoes`
- Configurável: Azure OpenAI (GPT-4o) ou Anthropic Claude via API
- Filtro de segurança: nunca enviar ao LLM conteúdo com nível de acesso Sigiloso

### 4.4 Zero Trust e Segurança Avançada
- **Autenticação contínua**: verificar contexto de risco a cada operação privilegiada
- **RBAC granular**: permissões por recurso + ação + unidade organizacional
- **Detecção de anomalias**: acesso anormal a processos, horário incomum, IP desconhecido
- **Criptografia em repouso**: documentos sigilosos com `AES-256-GCM`, chave derivada por processo
- **Conformidade LGPD**: anonimização de dados pessoais por decisão judicial ou prazo de retenção expirado

---

## Nível 4.5 — Plataforma Analítica e Inteligência Operacional

Objetivo: transformar o Novo SEI em fonte estratégica de dados para gestão institucional, comparável a soluções modernas de BPM Analytics.

### 4.5.1 Data Lake Operacional
Publicar todos os eventos de domínio para consumo analítico:
- Destinos configuráveis: **Azure Data Lake Storage Gen2**, **Delta Lake**, **MinIO** (on-premise), **Databricks**
- Formato de evento padronizado:
```json
{
  "evento": "ProcessoCriado",
  "tenant": "cef",
  "timestamp": "2025-06-07T10:30:00Z",
  "versao": "1.0",
  "payload": { }
}
```
- Eventos publicados: `ProcessoCriado`, `DocumentoCriado`, `DocumentoAssinado`, `ProcessoTramitado`, `ProcessoConcluido`, `SlaVencido`, `PrazoTemporalidadeVencido`, `DocumentoCorrompido`, `TramitacaoFederadaEnviada`
- Criar `IDataLakePublisher` com implementações para cada destino
- `DataLakePublisherWorker` consumindo a fila de eventos e publicando em batch

### 4.5.2 Event Streaming
- **MassTransit** como abstração principal (já previsto no Nível 4.2)
- Suporte opcional a **Apache Kafka** para volumes de alta escala via `MassTransit.Kafka`
- Tópicos separados por domínio: `novossei.processos`, `novossei.documentos`, `novossei.tramitacoes`, `novossei.arquivistica`
- Schema Registry para controle de versão dos contratos de evento (Avro ou JSON Schema)
- Replay de eventos: capacidade de reprocessar eventos a partir de um ponto no tempo

### 4.5.3 Dashboard Executivo
Criar projeto `NovoSei.Analytics` com módulo Blazor de visualização:

Indicadores obrigatórios:
- Tempo médio de tramitação por tipo de processo e por unidade
- Volume de processos criados/tramitados/concluídos por período
- Gargalos operacionais: processos parados por mais de X dias úteis sem movimentação
- SLA: percentual de cumprimento por unidade, processos vencidos, tendência
- Produtividade por área: documentos assinados, processos concluídos, tempo médio de resposta
- Taxa de assinaturas: A1 vs A3, por servidor, por período

Implementação:
- Componentes Blazor com gráficos via `ApexCharts.Blazor` ou `Radzen.Blazor`
- Dados pré-agregados em tabelas `Analytics_*` atualizadas por `AnalyticsAggregatorWorker` a cada 15 minutos
- Exportação para Excel (via `ClosedXML`) e PDF (via `QuestPDF`)
- Filtros por período, unidade, tipo de processo e tenant

### 4.5.4 Process Mining
Criar `IProcessMiningService` para descoberta e análise de fluxos reais:
- **Descoberta de fluxos**: a partir do log de tramitações, reconstruir o fluxo real executado por cada tipo de processo
- **Identificação de gargalos**: unidades com maior tempo médio de retenção de processos
- **Detecção de retrabalho**: processos que retornam a unidades já visitadas (ciclos no grafo de tramitação)
- **Análise de conformidade**: comparar fluxo real com fluxo BPMN modelado no Elsa Workflows, identificar desvios
- **Visualização**: renderizar grafo de processo com frequência e tempo médio em cada aresta

Exportações:
- **BPMN 2.0** com anotações de frequência e tempo
- **CSV** com log de eventos no formato XES (IEEE 1849-2023) para importação no ProM ou Celonis
- **Power BI**: dataset atualizado automaticamente via endpoint OData `GET /api/v1/analytics/processmining/odata`

### 4.5.5 Camada Semântica Corporativa
Construir índice organizacional unificado sobre todas as fontes de dados:
- **Fontes**: processos, documentos, metadados extraídos, eventos de workflow, histórico de tramitação
- **Índice unificado**: entidade `DocumentoIndexado` com embedding semântico + metadados estruturados
- Endpoint de busca corporativa: `POST /api/v1/busca/corporativa` com linguagem natural
- Pipeline de pergunta → reformulação → busca semântica → recuperação → geração de resposta (RAG)
- Restrição de acesso: resultado da busca filtrado pelo nível de acesso do usuário autenticado

### 4.5.6 Cockpit Estratégico para Alta Administração
Painel exclusivo para perfil `AltaAdministracao`:
- **Eficiência institucional**: índice composto de SLA + produtividade + tempo médio
- **Ranking de unidades**: por volume, por cumprimento de SLA, por tempo de resposta
- **Tendências operacionais**: séries temporais com previsão por modelo ARIMA simples
- **Capacidade instalada**: processos por servidor ativo, estimativa de saturação
- **Previsão de demanda**: projeção de volume para os próximos 30/60/90 dias com base em sazonalidade histórica
- **IA explicativa**: ao clicar em qualquer indicador, painel lateral com explicação gerada por LLM sobre causas e sugestões de ação
- Atualização em tempo real via SignalR para indicadores críticos (SLA vencendo, processos parados)

---

## Novo Projeto: NovoSei.PEN

Além dos projetos existentes, adicionar:
```
NovoSei.PEN/
├── Clients/
│   ├── IPenClient.cs
│   └── PenHttpClient.cs
├── Services/
│   ├── IPenProcessoService.cs
│   └── PenProcessoService.cs
├── Workers/
│   └── PenSyncWorker.cs
├── Models/
│   ├── ProcessoPen.cs
│   ├── TramitacaoFederada.cs
│   └── ProtocoloPen.cs
└── NovoSei.PEN.csproj  (referencia apenas NovoSei.Core)
```

## Novo Projeto: NovoSei.Arquivo

```
NovoSei.Arquivo/
├── Domain/
│   ├── PlanoClassificacao.cs
│   ├── TabelaTemporalidade.cs
│   ├── Acervo.cs
│   ├── LoteDocumental.cs
│   └── EventoArquivistico.cs
├── Services/
│   ├── IPreservacaoDigitalService.cs
│   ├── PreservacaoDigitalService.cs
│   ├── IGestaoAcervoService.cs
│   └── GestaoAcervoService.cs
├── Workers/
│   ├── TemporalidadeMonitorWorker.cs
│   └── PreservacaoVerificacaoWorker.cs
└── NovoSei.Arquivo.csproj  (referencia apenas NovoSei.Core)
```

## Novo Projeto: NovoSei.Analytics

```
NovoSei.Analytics/
├── Services/
│   ├── IProcessMiningService.cs
│   ├── ProcessMiningService.cs
│   ├── IDataLakePublisher.cs
│   └── DataLakePublisher.cs
├── Workers/
│   ├── AnalyticsAggregatorWorker.cs
│   └── DataLakePublisherWorker.cs
├── Models/
│   ├── IndicadorSla.cs
│   ├── IndicadorProdutividade.cs
│   └── GraficoProceso.cs
└── NovoSei.Analytics.csproj  (referencia apenas NovoSei.Core)
```

---

## Restrições e Padrões Globais

- **Nunca quebrar `IRepository<T>`** — extensões via novos métodos opcionais ou repositórios especializados
- **Migrations EF Core** para toda alteração de schema — sem scripts SQL avulsos
- **Feature Flags** via `Microsoft.FeatureManagement` para habilitar cada nível sem redeploy
- **Backward compatibility** na API pública — versionar antes de alterar contratos
- **Sem dependências circulares** — `Core` nunca referencia `Infrastructure`, `Web`, `PEN`, `Arquivo` ou `Analytics`
- **Novos projetos** (`PEN`, `Arquivo`, `Analytics`) referenciam apenas `Core`
- **Localização** pt-BR com `IStringLocalizer` em todos os componentes Blazor
- **Acessibilidade** WCAG AA: atributos ARIA, contraste, navegação por teclado
- **Conformidade legal**: e-ARQ Brasil, Decreto 10.278/2020, Resolução CONARQ nº 40/2014, LGPD, ICP-Brasil

---

## Entregáveis Esperados por Nível

Ao implementar cada nível, entregar:
1. Código-fonte completo dos novos serviços, interfaces, workers e componentes Blazor
2. Migration EF Core correspondente (se houver alteração de schema)
3. Testes unitários e de integração com cobertura mínima de 80% nas regras de negócio críticas
4. Atualização do `README.md` com instruções de configuração e variáveis de ambiente
5. Configuração de exemplo em `appsettings.Development.json` e `appsettings.Production.json`
6. Diagrama C4 (nível de container) atualizado refletindo os novos componentes adicionados
