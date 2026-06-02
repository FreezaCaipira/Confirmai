# 📋 Exploração Completa: Projeto Confirmai (ASP.NET Core 9.0 Blazor Server)

**Data:** 2 de junho, 2026  
**Versão do Framework:** ASP.NET Core 9.0, Blazor Server  
**Base de Dados:** PostgreSQL  
**Tipo:** Plataforma SaaS para confirmação de eventos, pagamentos e gerenciamento de grupos

---

## 📊 Sumário Executivo

| Métrica | Valor |
|---------|-------|
| **Total de Páginas (.razor)** | ~50 páginas |
| **Total de Componentes Shared** | 17 componentes |
| **Arquivos CSS** | 54 (4 globais + 50 scoped) |
| **Serviços** | 71+ serviços |
| **Hosted Services** | 6 serviços de background |
| **Componente Maior** | AdminPayments.razor (1.220 linhas) |
| **Componente Médio** | Groups/Detail.razor (1.041 linhas) |
| **Padrão de Autenticação** | ASP.NET Identity + Custom Claims |
| **Gateways de Pagamento** | 3 (BtcPay, AbacatePay, EfiBank, Appmax) |

---

## 1. 🏗️ Estrutura de Pastas e Organização

### 1.1 Estrutura Principal

```
Confirmai/
├── Pages/                    # Rotas e páginas Blazor
│   ├── Admin/               # Dashboard administrativo
│   ├── Auth/                # Páginas de autenticação (Identity)
│   ├── Payment/             # Fluxo de pagamentos
│   ├── Groups/              # Gerenciamento de grupos
│   ├── Futsal/              # Agendamento de jogos (futsal)
│   ├── Poker/               # Torneios de poker
│   ├── VenueManager/        # Gerenciamento de locais
│   ├── Product/             # Marketplace de produtos
│   ├── MyEvents/            # Meus eventos
│   ├── MyConfirmations/     # Confirmações do usuário
│   ├── Docs/                # Documentação (integração API)
│   ├── Dashboard.razor      # Resumo do usuário
│   ├── Profile.razor        # Perfil do usuário
│   ├── Users.razor          # Listagem de usuários
│   ├── Mailbox.razor        # Sistema de mensagens
│   ├── Contact.razor        # Formulário de contato
│   └── Index.razor          # Home/landing
├── Shared/                  # Componentes compartilhados
│   ├── Components/          # 17 componentes Blazor
│   ├── Helpers/             # Funções auxiliares
│   └── RouteNotAuthorized.razor
├── Services/                # Lógica de negócio (71+ serviços)
│   ├── Payment/             # Serviços de pagamento
│   ├── Factories/           # Factory patterns
│   └── Interfaces/          # Contratos de serviço
├── Data/                    # EF Core DbContext
├── Models/                  # Entidades de dados
├── Configuration/           # Configurações de segurança
├── Enums/                   # Enumerações (Sport, Role, etc.)
├── Hubs/                    # SignalR (PaymentHub)
├── Endpoints/               # (não encontrado - pode estar vazio)
├── wwwroot/
│   ├── css/                 # 4 arquivos CSS globais
│   ├── js/                  # JavaScript (navProgress, cookies, etc.)
│   ├── images/              # Imagens estáticas
│   ├── uploads/             # Uploads de usuários
│   └── evidences/           # Provas de pagamento
└── Areas/
    └── Identity/            # Scaffolded Identity UI
```

### 1.2 Organização por Domínio

O projeto segue **organização por feature/domínio**:

- **Pagamentos**: `Pages/Payment/`, `Services/*PaymentService`, `Services/*WebhookService`
- **Eventos**: `Pages/Futsal/`, `Pages/Poker/`, `Services/EventNotification*`
- **Grupos**: `Pages/Groups/`, `Services/EventCollisionService`
- **Admin**: `Pages/Admin/`, `Services/Admin*.cs`
- **Autenticação**: `Areas/Identity/`, `Services/CustomClaimsPrincipalFactory`, `Services/AuthNavigationHelper`

### 1.3 Convenções de Nomenclatura

| Artefato | Padrão | Exemplo |
|----------|--------|---------|
| **Páginas** | PascalCase | `AdminPayments.razor`, `GroupDetail.razor` |
| **Componentes** | PascalCase | `MainLayout.razor`, `Toast.razor` |
| **Serviços** | `{Domain}Service` | `PaymentConfirmationService`, `AdminLogsQueryService` |
| **Interfaces** | `I{ServiceName}` | `IBitcoinPaymentService`, `IEventPaymentGateway` |
| **Models** | PascalCase | `PaymentRecord`, `ApplicationUser` |
| **Enums** | PascalCase | `Sport`, `GroupMemberRole`, `PaymentStatus` |
| **CSS Classes** | kebab-case + BEM | `.entity-shell`, `.entity-shell__card`, `.entity-shell--active` |
| **CSS Scoped** | `{ComponentName}.razor.css` | `MainLayout.razor.css` |

### 1.4 Características de Organização

✅ **Boas Práticas Observadas:**
- Separação clara entre componentes de layout (Shared) e páginas (Pages)
- Padrão consistente de nomenclatura
- Componentes reutilizáveis em `Shared/Components/`
- Serviços bem definidos com responsabilidades claras

⚠️ **Áreas para Melhorias:**
- Componentes muito grandes deveriam ser divididos (ver seção 3)
- Falta de subpastas em `Services/` para melhor organização
- Muitos serviços no nível raiz de `Services/`

---

## 2. 🎨 Estrutura CSS

### 2.1 Organização de Arquivos CSS

**Arquivos CSS Globais (4):**
```
wwwroot/css/
├── site.css               # Design tokens, layout, shells, tabelas, botões
├── events.css             # Estilos compartilhados (Futsal + Poker)
├── identity.css           # Estilos para páginas de autenticação
└── marketplace.css        # Estilos do marketplace (produtos)
```

**CSS Scoped por Componente (50):**
```
Pages/Payment/Payment.razor.css
Pages/Groups/Detail.razor.css
Shared/Components/MainLayout.razor.css
... (total: 50 arquivos)
```

### 2.2 Padrão de Nomenclatura CSS

**Convenção Utilizada:** BEM (Block Element Modifier) + kebab-case

**Exemplos:**
```css
/* Block */
.entity-shell { }

/* Element */
.entity-shell-grid { }
.entity-shell-card { }
.entity-shell-main { }

/* Modifier */
.entity-shell-main--active { }
.entity-shell-card--loading { }

/* Alternative: prefix-based */
.tibia-pay-grid { }
.tibia-pay-card { }
.tibia-pay-card-head { }
.tibia-pay-item-card { }
```

### 2.3 Design Tokens (CSS Custom Properties)

**Definidos em `site.css` (linha 18+):**

```css
:root {
  /* Dark Backgrounds (Tibia-inspired) */
  --bg-deepest: #2f1a09;
  --bg-deep: #2f1d0b;
  --bg-dark: #3d2d1d;
  
  /* Confirmai Navy (NEW STANDARD - use these!) */
  --ci-bg:           #090f18;
  --ci-bg-card:      #111927;
  --ci-bg-input:     #07111d;
  --ci-accent:       #4f9cf8;
  --ci-border:       #1b3d6c;
  
  /* Parchment Colors (warm tones) */
  --parchment: #efd6ac;
  --parchment-light: #f0dbb4;
  
  /* Status Colors */
  --green: #6e9a3f;
  --red: #e53935;
  
  /* Typography */
  --font-display: "Cinzel", Georgia, "Times New Roman", serif;
  --font-accent: "MedievalSharp", Georgia, cursive;
}
```

### 2.4 Padrão Scoped vs Global

| Escopo | Uso | Exemplos |
|--------|-----|----------|
| **Global (site.css)** | Layout shell, design tokens, sistema de botões, tabelas | `.main`, `.entity-shell`, `.btn` |
| **Global (events.css)** | Esqueletos de carregamento, states compartilhados | `.detail-skeleton`, `.detail-loading` |
| **Scoped (.razor.css)** | Estilos específicos do componente | `MainLayout.razor.css`, `Payment.razor.css` |

### 2.5 Análise de CSS

**Estrutura de site.css (preview - 1º 100 linhas):**

```css
/* Design tokens */
:root { ... }           /* ~80 linhas - variáveis de cor e tipografia */

/* Base styles */
html { ... }            /* Reset, fonts */
body { ... }            /* Dark bg, text color */

/* Layout shell */
.main { ... }           /* Margin-left: 220px (sidebar) */
.sidebar { ... }        /* Fixed left navigation */
header { ... }          /* Dark header com border-bottom */

/* Animations */
@keyframes shimmer { } /* Loading skeleton animation */
@keyframes pulse { }   /* Pulse effect */

/* Button system */
.btn { }                /* Button base */
.btn-primary { }        /* Primary buttons */
.btn-ghost { }          /* Ghost/outline buttons */

/* Entity shell system */
.entity-shell { }       /* Main container */
.entity-shell-grid { }  /* Grid layout */
.entity-shell-card { }  /* Card styling */
```

### 2.6 Boas Práticas Observadas

✅ **Positivo:**
- Uso de design tokens (CSS custom properties)
- Organização clara de estilos globais vs scoped
- Convenção BEM consistente
- Scoped CSS evita conflitos
- Responsive design com media queries

⚠️ **Problemas Identificados:**

1. **Duplicação de Estilos:**
   - Muitos componentes têm estilos similares (cards, shells, borders)
   - Possível consolidação em classes utilitárias

2. **Múltiplas Paletas de Cores:**
   - Estilos "Tibia" (parchment, brown) vs "Confirmai" (navy, blue)
   - Mensagem conflitante: comentário diz "use these for all new code" mas código ainda usa cores antigas
   - **Recomendação**: Migração gradual para `--ci-*` tokens

3. **Falta de Variáveis para Propriedades Comuns:**
   - Valores como `border-radius: 16px` repetidos sem variável
   - Valores de sombra (box-shadow) repetidos

4. **CSS em Componentes Muito Grandes:**
   - Alguns `.razor.css` podem ter 200+ linhas
   - Dificulta manutenção

### 2.7 Consolidação de CSS Recomendada

**Exemplos de Duplicação Identificada:**

```css
/* Em site.css e múltiplos .razor.css */
border: 1px solid #1b3d6c;
border-radius: 16px;
background: linear-gradient(180deg, #111927 0%, #0d1825 100%);
box-shadow: inset 0 -1px 0 rgba(26, 90, 176, 0.18);
```

**Solução Proposta:**
```css
/* Criar classe utilitária */
:root {
  --card-border: 1px solid #1b3d6c;
  --card-radius: 16px;
  --card-bg-gradient: linear-gradient(180deg, #111927 0%, #0d1825 100%);
  --card-shadow-inset: inset 0 -1px 0 rgba(26, 90, 176, 0.18);
  --transition-fast: 0.15s ease;
  --transition-normal: 0.2s ease;
}

.card-base {
  border: var(--card-border);
  border-radius: var(--card-radius);
  background: var(--card-bg-gradient);
  box-shadow: var(--card-shadow-inset);
}
```

---

## 3. 🔧 Componentes Blazor

### 3.1 Distribuição de Componentes

**Componentes Shared (17 componentes reusáveis):**

| Componente | Responsabilidade | Tamanho |
|------------|------------------|--------|
| `MainLayout.razor` | Layout principal, sidebar, header, auth state | 323 linhas |
| `Toast.razor` | Sistema de notificações toast | ~50 linhas |
| `Header.razor` | Cabeçalho com navegação | ~100 linhas |
| `Footer.razor` | Rodapé | ~30 linhas |
| `CookieConsent.razor` | Banner de consentimento de cookies | ~70 linhas |
| `Breadcrumb.razor` | Navegação breadcrumb | ~40 linhas |
| `PaginationControls.razor` | Controles de paginação | ~30 linhas |
| `StatCard.razor` | Card para estatísticas | ~20 linhas |
| `UserSummaryCard.razor` | Card de resumo do usuário | ~60 linhas |
| `QRCode.razor` | Gerador de QR code | ~30 linhas |
| `BtcQuoteCard.razor` | Cotação de Bitcoin em tempo real | ~80 linhas |
| `EventListingShell.razor` | Shell para listagem de eventos | ~170 linhas |
| `EntityProfileShell.razor` | Shell para perfil de entidade | ~100 linhas |
| `AdminDataState.razor` | Gerencia estados (loading, empty, error) | ~50 linhas |
| `ActivePaymentMethodsWidget.razor` | Widget de métodos de pagamento ativos | ~40 linhas |
| `FilterActionButtons.razor` | Botões de filtro/ação | ~20 linhas |
| `RouteNotAuthorized.razor` | Página de acesso negado | ~30 linhas |

**Páginas Grandes (problemas de tamanho):**

| Página | Linhas | Problema |
|--------|--------|---------|
| `Admin/AdminPayments.razor` | **1.220** | ⚠️ Muito grande - combina lista, filtros, modals, lógica complexa |
| `Groups/Detail.razor` | **1.041** | ⚠️ Muito grande - perfil completo + edição + membros |
| `Futsal/Escalacao.razor` | **820** | ⚠️ Grande - escalação de time + lógica de arraste |
| `Payment/Payment.razor` | **707** | ⚠️ Grande - checkout com múltiplos gateways |
| `Admin/AdminLogs.razor` | ~600+ | ⚠️ Grande - log viewer com filtros |

### 3.2 Análise de Component Parameters

**Exemplo de Bom Uso (PaginationControls.razor):**
```razor
[Parameter] public int CurrentPage { get; set; } = 1;
[Parameter] public int TotalPages { get; set; } = 1;
[Parameter] public EventCallback OnPrevious { get; set; }
[Parameter] public EventCallback OnNext { get; set; }
[Parameter] public string PreviousLabel { get; set; } = "Anterior";
```

**Padrões Identificados:**
- ✅ Parameters bem definidos com defaults
- ✅ EventCallbacks para comunicação pai-filho
- ✅ Documentação clara através de nomes

### 3.3 Cascading Parameters

**Cascading Values Implementados:**

1. **Toast (MainLayout → Pages)**
   ```razor
   <!-- MainLayout.razor -->
   <CascadingValue Value="ToastRef">
     @Body
   </CascadingValue>
   
   <!-- Pages/Payment/Payment.razor -->
   [CascadingParameter] public Toast? ToastRef { get; set; }
   ```

2. **AuthenticationState (Built-in)**
   ```razor
   <CascadingAuthenticationState>
     @Body
   </CascadingAuthenticationState>
   ```

**Boas Práticas:**
- ✅ Minimal uso (apenas Toast necessário)
- ✅ Não abusam de cascading parameters
- ⚠️ Poderiam usar para CurrencyPreference e LanguagePreference (atualmente injetos via serviço)

### 3.4 Lifecycle e State Management

**Padrões Observados:**

```csharp
// 1. OnInitializedAsync - Carregamento de dados
protected override async Task OnInitializedAsync()
{
    userId = (await AuthStateProvider.GetAuthenticationStateAsync())
        .User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    
    if (!string.IsNullOrEmpty(userId))
        await LoadEventsAsync();
}

// 2. OnParametersSetAsync - Recarregar quando parâmetros mudam
protected override async Task OnParametersSetAsync()
{
    if (Id != previousId)
    {
        previousId = Id;
        await LoadDataAsync();
    }
}

// 3. StateHasChanged - Atualização manual (100+ ocorrências!)
StateHasChanged();  // ⚠️ Overuse indicado

// 4. IAsyncDisposable - Limpeza de recursos
@implements IAsyncDisposable

async ValueTask IAsyncDisposable.DisposeAsync()
{
    await _circuit.DisconnectAsync();
}
```

**Análise:**
- ✅ Uso correto de OnInitializedAsync para carregamento
- ⚠️ **PROBLEMA**: Mais de 100 chamadas a `StateHasChanged()` encontradas
  - Indica re-rendering excessivo ou debouncing inadequado
  - Potencial problema de performance
- ✅ IAsyncDisposable em 3 páginas (VenueEdit, AdminPayments, AdminVenueEdit)
- ⚠️ Disposable pattern poderia ser mais consistente

### 3.5 Re-rendering e Performance

**Problemas Identificados:**

```razor
<!-- Anti-pattern encontrado em Poker/Index.razor -->
@onfocusout="() => Task.Delay(150).ContinueWith(_ => { 
    showTypeMenu = false; 
    InvokeAsync(StateHasChanged);  // ⚠️ StateHasChanged dentro de setTimeout
})"
```

**Recomendação:**
```razor
<!-- Melhor abordagem -->
private async Task OnFocusOut()
{
    await Task.Delay(150);
    showTypeMenu = false;
    // StateHasChanged is called automatically by Blazor
}

@onfocusout="OnFocusOut"
```

### 3.6 JS Interop

**Uso Identificado:**

1. **LocalStorage:**
   ```csharp
   await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "Confirmai.uiLanguage")
   ```

2. **Navegação:**
   ```csharp
   await JSRuntime.InvokeVoidAsync("navProgress.finish");
   ```

3. **Cookie Management:**
   ```csharp
   await JSRuntime.InvokeVoidAsync("ConfirmaiSetCookie", "Confirmai.uiLanguage", language, 365);
   ```

**Padrão Abstrato Bom:**
```csharp
// LocalStorageStateHelpers.cs - Centraliza lógica
public static async Task<string> GetStringAsync(IJSRuntime js, string key)
    => await js.InvokeAsync<string?>("localStorage.getItem", key) ?? string.Empty;
```

### 3.7 Problemas de Componentes

| Problema | Localização | Impacto |
|----------|-------------|---------|
| Componente muito grande | AdminPayments (1.220 linhas) | Difícil manutenção, alto acoplamento |
| Múltiplos StateHasChanged | Payment pages | Performance degradada |
| Task.Delay sem CancellationToken | EventPayment.razor linha 418 | Pode vazar recursos |
| Sem timeout em await | Algumas chamadas | Pode travar UI |
| Muita lógica no @code | Todos os componentes grandes | Difícil testar |

---

## 4. 🔐 Serviços e Lógica de Negócio

### 4.1 Arquitetura de Serviços

**Serviços por Categoria (71+ serviços):**

#### 🔐 **Autenticação & Autorização (5)**
- `CustomClaimsPrincipalFactory.cs` - Claims customizados
- `AuthNavigationHelper.cs` - Redirecionamento pós-login
- `RevalidatingIdentityAuthenticationStateProvider.cs` - Validação de autenticação
- `IdentityEmailSender.cs` - Envio de emails
- `PiiSanitizer.cs` - Sanitização de dados sensíveis

#### 💳 **Pagamentos (15+)**
- `PaymentConfirmationService.cs` - Confirmação de pagamentos
- `BitcoinPaymentFactory.cs` - Factory para múltiplos gateways
- `BtcPayServerPaymentService.cs` - Integração BtcPay
- `TestnetBitcoinPaymentService.cs` - Bitcoin testnet
- `AbacatePayPixService.cs` - PIX via AbacatePay
- `AppmaxPixService.cs` - PIX via Appmax
- `EfiBankPixService.cs` - PIX via Efi Bank
- `BtcPayWebhookService.cs` - Webhooks BtcPay
- `AbacatePayWebhookService.cs` - Webhooks AbacatePay
- `EfiBankWebhookService.cs` - Webhooks EfiBank
- `GatewayService.cs` - Coordenador de gateways
- `OperationFeeCalculatorService.cs` - Cálculo de taxas
- `CryptoQuoteService.cs` - Cotações de crypto
- `BitcoinQuoteService.cs` - Cotações de Bitcoin
- `EventPaymentReconciliationService.cs` - Reconciliação de pagamentos

#### 📊 **Admin & Auditoria (15+)**
- `AdminConfirmationService.cs` - Gerenciamento de confirmações
- `AdminLogFiltering.cs`, `AdminLogSorting.cs`, `AdminLogsQueryService.cs` - Logs
- `AdminLogsFilterStateService.cs` - Estado de filtros
- `AdminPaymentsFilterStateService.cs` - Filtros de pagamentos
- `AdminUsersFilterStateService.cs` - Filtros de usuários
- `AdminSecurityPolicyService.cs` - Políticas de segurança
- `AdminSettingsService.cs` - Configurações admin
- `AdminLogsExportService.cs` - Exportação de logs
- `AdminLogsDeepLinkBuilder.cs` - Deep linking para logs

#### 🎮 **Eventos & Games (10+)**
- `EventCollisionService.cs` - Detecção de conflitos
- `EventNotificationService.cs` - Notificações de eventos
- `EventNotificationSchedulerService.cs` - Agendador
- `EventPaymentReconciliationWorker.cs` - Reconciliação assíncrona
- `RachaSchedulerService.cs` - Agendamento de eventos (racha = jogo)
- `DelinquencyService.cs` - Controle de delinquência
- `DelinquencyNotificationTests.cs` - Notificações

#### 👤 **Usuários & Perfil (5)**
- `UserService.cs` - Operações de usuário
- `CurrencyPreferenceService.cs` - Preferência de moeda
- `LanguagePreferenceService.cs` - Preferência de idioma
- `UiTextService.cs` - Internacionalização
- `AppInitializationService.cs` - Inicialização da app

#### 📈 **Métricas & Logs (5)**
- `LogService.cs` - Sistema de logging
- `LogRetentionService.cs` - Retenção de logs
- `DashboardMetricsService.cs` - Métricas do dashboard
- `PaymentDomainMetrics.cs` - Métricas de pagamento
- `PendingWebhooksAlertService.cs` - Alerta de webhooks

#### 🎯 **Outros (6)**
- `ProductService.cs` - Gerenciamento de produtos
- `PixProofUploadService.cs` - Upload de comprovantes PIX
- `LocalStorageStateHelpers.cs` - LocalStorage helpers
- `MailboxConversationArchiveService.cs` - Arquivamento de mensagens
- `CertificateHealthCheckService.cs` - Saúde de certificados
- `DebounceDispatcher.cs` - Debouncing de ações

### 4.2 Padrões de Dependency Injection

**Configuração em Program.cs:**

```csharp
// Singletons (compartilhados globalmente)
builder.Services.AddSingleton<BitcoinQuoteService>();
builder.Services.AddSingleton<CryptoQuoteService>();
builder.Services.AddSingleton<PaymentEventBus>();
builder.Services.AddSingleton<PaymentDomainMetrics>();

// Scoped (por requisição/circuito)
builder.Services.AddScoped<IBitcoinPaymentService, BtcPayServerPaymentService>();
builder.Services.AddScoped<IBitcoinPaymentService, TestnetBitcoinPaymentService>();
builder.Services.AddScoped<IEventPaymentGateway, EfiBankEventPaymentGateway>();

// Hosted Services (background workers)
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddHostedService<RachaSchedulerService>();
```

✅ **Boas Práticas:**
- Interfaces bem definidas
- Factories para múltiplas implementações
- Separação clara de Singletons vs Scoped

### 4.3 Padrões de Serviço

**Exemplo: PaymentConfirmationService.cs**
```csharp
public class PaymentConfirmationService
{
    // Dependências injetadas
    public PaymentConfirmationService(
        AppDbContext db,
        BitcoinPaymentFactory paymentFactory,
        LogService logService,
        IHubContext<PaymentHub> hubContext,
        PaymentEventBus eventBus)

    // Método público com retorno estruturado
    public async Task<(bool Confirmed, bool AlreadyPaid, decimal ReceivedAmount)> 
        ConfirmAsync(PaymentRecord payment)
    {
        // 1. Validação
        if (dbPayment == null) return (false, false, 0m);
        
        // 2. Delegação para factory
        var service = _paymentFactory.GetService(paymentMethod);
        
        // 3. Lógica de negócio
        var received = await service.GetReceivedAmountAsync(parameter);
        
        // 4. Persistência
        await _db.SaveChangesAsync();
        
        // 5. Notificação
        await _hubContext.Clients.User(userId).SendAsync("PaymentConfirmed", ...);
        
        // 6. Event bus
        _eventBus.NotifyPaymentConfirmed(...);
    }
}
```

✅ **Padrões Bons:**
- Responsabilidade única
- Injeção de dependências clara
- Retorno estruturado (tuples)
- Logging integrado
- Eventos publicados via EventBus

### 4.4 Serviços Críticos

| Serviço | Criticalidade | Responsabilidade | Testes |
|---------|---------------|------------------|--------|
| `PaymentConfirmationService` | **CRÍTICO** | Confirmação de pagamentos | ✅ Testado |
| `AdminConfirmationService` | **CRÍTICO** | Confirmação de eventos | ✅ Testado |
| `DelinquencyService` | **CRÍTICO** | Controle de inadimplência | ✅ Testado |
| `GatewayService` | **ALTO** | Coordenação de gateways | Parcial |
| `EventNotificationSchedulerService` | **ALTO** | Notificações assíncronas | Parcial |
| `BitcoinQuoteService` | **MÉDIO** | Cotações em tempo real | ✅ Testado |

### 4.5 Problemas Identificados

| Problema | Impacto | Localização |
|----------|---------|-------------|
| 71 serviços no nível raiz de `Services/` | Difícil navegação | `Services/*.cs` |
| Falta de subpastas organizacionais | Escalabilidade | `Services/` |
| Alguns serviços podem ter responsabilidades múltiplas | Manutenção | AdminLogs* (5 arquivos) |
| Task.Delay sem CancellationToken | Vazamento de recursos | RachaSchedulerService, LogRetentionService |
| Pouca documentação em serviços críticos | Manutenção | PaymentConfirmationService |

---

## 5. 🔒 Segurança

### 5.1 Mecanismos de Autenticação

**Framework:** ASP.NET Identity + Custom Claims

```csharp
// Program.cs - Configuração
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = securityPolicy.RequireConfirmedEmail && emailEnabled;
    options.Password.RequiredLength = 10; // Produção
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();
```

**Segurança Stamp Validation:**
```csharp
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Valida a cada 30 segundos
    // Permite kick-out rápido após UpdateSecurityStampAsync
    options.ValidationInterval = TimeSpan.FromSeconds(30);
});
```

✅ **Bom:** Validação de segurança a cada 30s evita sessões comprometidas

### 5.2 Autorização

**Políticas de Autorização:**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ServerAdmin", policy =>
        policy.RequireClaim("server_admin", "true"));
});
```

**Proteção de Rotas:**

| Rota | Proteção | Requisito |
|------|----------|-----------|
| `/payments` | `[Authorize]` | Autenticado |
| `/admin/*` | `[Authorize]` + Policy | Server Admin |
| `/grupo/{id}` | `[AllowAnonymous]` | Público (detalhes legível) |
| `/futsal/create` | `[Authorize]` | Autenticado |
| `/poker/detail/{id}` | `[AllowAnonymous]` | Público (ver torneio) |
| `/marketplace/buy/{id}` | `[Authorize]` | Autenticado |

✅ **Bom:**
- Proteção explícita com `[Authorize]`
- Detalhes públicos para visualização
- Operações protegidas (Create, Edit, Delete)

⚠️ **Possível Melhoria:**
- Falta de Role-based authorization em algumas rotas
- Admin routes poderiam usar `[Authorize(Policy = "ServerAdmin")]`

### 5.3 Configuração de Cookies

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Confirmai.session";
    options.Cookie.HttpOnly = true;
    
    // Produção: Always | Dev: SameAsRequest
    options.Cookie.SecurePolicy = isDevelopment 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    
    // Produção: Strict | Dev: Lax
    options.Cookie.SameSite = isDevelopment 
        ? SameSiteMode.Lax 
        : SameSiteMode.Strict;
    
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Produção
    options.LoginPath = "/Identity/Account/Login";
});
```

✅ **Excelente:**
- HttpOnly (previne XSS)
- Secure (HTTPS only em produção)
- SameSite Strict (previne CSRF)
- Sliding expiration (UX melhorada)
- Timeout de 30 minutos

### 5.4 Proteção contra CSRF

**Data Protection com Database:**
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();
```

✅ **Excelente:** Antiforgery tokens persistem em banco, válidos após restart

### 5.5 Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddFixedWindowLimiter("webhook", o =>
    {
        o.PermitLimit = 30;  // 30 requisições por janela
        o.Window = TimeSpan.FromMinutes(1);
    });
});
```

✅ **Bom:** Proteção de webhooks contra abuso

### 5.6 API Key Authentication

```csharp
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(
        ApiKeyAuthDefaults.AuthenticationScheme, _ => { });
```

**Implementação em [Configuration/ApiKeyAuth.cs](Configuration/ApiKeyAuth.cs)**

✅ **Bom:** Suporte a autenticação via API Key para webhooks/integrações

### 5.7 Certificados SSL

```csharp
var efiBankCfg = builder.Configuration.GetSection(EfiBankOptions.Section)
    .Get<EfiBankOptions>();

if (!string.IsNullOrWhiteSpace(efiBankCfg?.WebhookClientCertSubject))
{
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.ConfigureHttpsDefaults(https =>
        {
            https.ClientCertificateMode = 
                Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
        });
    });
}
```

✅ **Excelente:** mTLS para webhooks EfiBank (validação de certificado cliente)

### 5.8 Validação de Dados

⚠️ **Não verificado em detalhes**, mas boas práticas ASP.NET:
- DataAnnotations em Models
- ModelState validation em Controllers
- Server-side validation obrigatório

**Recomendação:** Verificar validação de:
- Inputs de formulário
- Payloads de webhook
- Query strings (IDs)

### 5.9 Proteção contra XSS

✅ **Blazor Server oferece proteção nativa:**
- Content encoding automático
- Sem eval() ou innerHTML
- JS Interop restrito

**Possível Risco:**
```csharp
// Se usado: @Html.Raw(userInput) ⚠️ Inseguro
// Correto: @userInput ✅ Automatically encoded
```

### 5.10 Proteção de Dados Sensíveis

**Sanitização em PiiSanitizer.cs:**
- Remover/mascarar dados sensíveis em logs
- GDPR compliance

### 5.11 Resumo de Segurança

| Aspecto | Status | Nota |
|--------|--------|------|
| Autenticação | ✅ Forte | Identity + Custom Claims + Security Stamp |
| Autorização | ✅ Bom | Atributos [Authorize] bem aplicados |
| CSRF | ✅ Protegido | Antiforgery com persistência em DB |
| Cookies | ✅ Seguro | HttpOnly + Secure + SameSite |
| Rate Limiting | ✅ Implementado | Para webhooks |
| mTLS | ✅ Suportado | Para EfiBank webhooks |
| XSS | ✅ Mitigado | Content encoding automático |
| SQL Injection | ✅ Seguro | EF Core + Parameterized queries |
| Secrets | ⚠️ Bom | User Secrets em dev, considerar vault em prod |
| HTTPS | ⚠️ Esperado | Secure cookies só em HTTPS |

---

## 6. ⚡ Boas Práticas Blazor Server

### 6.1 StateHasChanged() - Overuse Identificado

**Problema Crítico:**
```
101 matches para "StateHasChanged()" encontradas
```

**Localização:**
- `MainLayout.razor`: 8 chamadas
- `Payment.razor`: 5 chamadas
- `EventPayment.razor`: 8 chamadas
- `AdminPayments.razor`: 5 chamadas
- etc.

**Análise:**
```razor
// ⚠️ Anti-pattern
private void OnSomeEvent()
{
    // Atualiza dados
    isLoading = false;
    
    // Força render manual
    StateHasChanged();  // ❌ Não deveria ser necessário
}

// ✅ Correto - Blazor re-renderiza automaticamente após eventos
private async Task OnSomeEventAsync()
{
    await SomeServiceAsync();  // Após await, renderização acontece
    isLoading = false;  // Mudança de estado causa re-render
    // StateHasChanged() aqui é desnecessário!
}
```

**Quando StateHasChanged() é necessário:**
1. **Operações síncronas não-UI:**
   ```csharp
   private void OnButtonClick()
   {
       var result = _service.SyncMethod();  // Síncrono, sem await
       isLoading = result;
       StateHasChanged();  // Necessário aqui
   }
   ```

2. **Callbacks de eventos externos (JS Interop, SignalR):**
   ```csharp
   public async Task OnExternalNotification(string data)
   {
       _data = data;
       await InvokeAsync(StateHasChanged);  // Thread-safe
   }
   ```

**Recomendação:**
- Refatorar para async/await onde possível
- Usar `InvokeAsync(StateHasChanged)` thread-safe
- Debouncing de atualizações frequentes

### 6.2 Circuits e Timeouts

**Configuração em Program.cs:**
```csharp
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});
```

✅ **Bom:**
- Retém 100 circuits desconectados por 3 minutos
- Timeout de 60s para JS Interop (padrão é 1 minuto)
- Batches buffered controlado

⚠️ **Considerações:**
- 100 circuits × 3 minutos = consumo de memória
- Em produção com alto tráfego, pode ser problema
- Monitorar uso de memória

### 6.3 JS Interop

**Padrão Bom Identificado:**
```csharp
// LocalStorageStateHelpers.cs - Centraliza chamadas
public static async Task<string> GetStringAsync(IJSRuntime js, string key)
{
    try
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", key) 
            ?? string.Empty;
    }
    catch (TaskCanceledException)
    {
        // Handle timeout
        return string.Empty;
    }
}
```

✅ **Bom:**
- Abstraído em helpers
- Error handling
- Reutilizável

⚠️ **Problemas Identificados:**
```csharp
// Em MainLayout.razor
try { await JSRuntime.InvokeVoidAsync("navProgress.finish"); } 
catch { }  // ⚠️ Silenciar exceção não é bom

// Melhor:
try { await JSRuntime.InvokeVoidAsync("navProgress.finish"); }
catch (TaskCanceledException) 
{ 
    Logger.LogWarning("navProgress.finish timeout");
}
```

### 6.4 Async/Await Patterns

**Padrão Correto (OnInitializedAsync):**
```csharp
protected override async Task OnInitializedAsync()
{
    // Bloqueia renderização até completar
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    
    if (!string.IsNullOrEmpty(userId))
        await LoadDataAsync();
}
```

✅ **Correto:**
- Aguarda dados necessários
- Não renderiza UI sem dados
- Evita race conditions

⚠️ **Anti-pattern encontrado:**
```csharp
// Em Payment.razor
_ = Task.Delay(1500);  // ⚠️ Fire-and-forget sem await
```

**Melhor:**
```csharp
await Task.Delay(1500, cancellationToken);
```

### 6.5 Memory Leaks

**IAsyncDisposable - Bem Implementado:**
```csharp
@implements IAsyncDisposable

private IAsyncDisposable? _subscription;

protected override async Task OnInitializedAsync()
{
    _subscription = await SomeAsync();
}

async ValueTask IAsyncDisposable.DisposeAsync()
{
    if (_subscription is not null)
        await _subscription.DisposeAsync();
}
```

✅ **Bom:** 3 páginas implementam (VenueEdit, AdminVenueEdit, AdminPayments)

⚠️ **Falta em outras:** Muitas páginas com long-lived subscriptions sem cleanup

### 6.6 Re-rendering Performance

**Problema Identificado:**
```razor
<!-- Em Pages/Poker/Index.razor -->
@onfocusout="() => Task.Delay(150)
    .ContinueWith(_ => { 
        showTypeMenu = false; 
        InvokeAsync(StateHasChanged);  // ⚠️ Desnecessário
    })"
```

**Impacto:**
- Re-renders excessivos
- UI flicker possível
- Consumo desnecessário de CPU

**Solução:**
```csharp
private async Task OnMenuFocusOut()
{
    await Task.Delay(150);
    showTypeMenu = false;
    // Blazor renderiza automaticamente após await
}
```

### 6.7 Resumo Blazor Server

| Aspecto | Status | Nota |
|---------|--------|------|
| StateHasChanged | ⚠️ Overuse | 101 chamadas, muitas desnecessárias |
| Async/Await | ✅ Bom | Padrão consistente |
| JS Interop | ✅ Centralizado | LocalStorageStateHelpers helpers |
| IAsyncDisposable | ⚠️ Parcial | Apenas 3 páginas implementam |
| Memory Leaks | ⚠️ Risco | Sem cleanup em muitos lugares |
| Performance | ⚠️ Preocupante | Re-renders excessivos |
| Circuits | ✅ Configurado | 100 × 3 min, considerar produção |

---

## 7. 🔧 Áreas para Melhorias

### 7.1 Refatoração de Componentes Grandes

**Componentes problemáticos:**

| Componente | Linhas | Problema | Solução |
|-----------|--------|---------|---------|
| `AdminPayments.razor` | 1.220 | Muito grande, muitas responsabilidades | Dividir em 3-4 sub-componentes |
| `Groups/Detail.razor` | 1.041 | Perfil + edição + membros combinados | Extrair em componentes |
| `Futsal/Escalacao.razor` | 820 | Lógica de arraste + estado | Componentizar tabela e dragdrop |
| `Payment.razor` | 707 | Múltiplos gateways + checkout | Extrair gateway selector |

**Exemplo de Refatoração (AdminPayments.razor):**

Dividir em:
```
AdminPayments.razor (container principal, 300 linhas)
├── AdminPaymentsList.razor (tabela, 400 linhas)
├── AdminPaymentFilter.razor (filtros, 200 linhas)
├── AdminPaymentModal.razor (modal de detalhes, 200 linhas)
└── AdminPaymentSummary.razor (resumo estatístico, 120 linhas)
```

### 7.2 Consolidação CSS

**Problema:** Duplicação de estilos

**Solução Proposta:**

1. **Criar arquivo de utilitários:**
```css
/* wwwroot/css/utilities.css */
:root {
  /* Cards */
  --card-border: 1px solid #1b3d6c;
  --card-radius: 16px;
  --card-bg: linear-gradient(180deg, #111927 0%, #0d1825 100%);
  --card-shadow: inset 0 -1px 0 rgba(26, 90, 176, 0.18);
  
  /* Transitions */
  --transition-fast: 0.15s ease;
  --transition-normal: 0.2s ease;
  
  /* Spacing */
  --gap-xs: 0.25rem;
  --gap-sm: 0.5rem;
  --gap-md: 1rem;
}

.card-base {
  border: var(--card-border);
  border-radius: var(--card-radius);
  background: var(--card-bg);
  box-shadow: var(--card-shadow);
  transition: all var(--transition-normal);
}

.gap-flex-md {
  display: flex;
  gap: var(--gap-md);
}
```

2. **Migrar cores "Tibia" para "Confirmai"**
   - Estabelecer timeline de deprecação
   - Documentar novos tokens
   - Atualizar componentes gradualmente

### 7.3 Organização de Serviços

**Problema:** 71 serviços no nível raiz

**Solução:**
```
Services/
├── Authentication/
│   ├── CustomClaimsPrincipalFactory.cs
│   ├── AuthNavigationHelper.cs
│   └── IdentityEmailSender.cs
├── Payments/
│   ├── PaymentConfirmationService.cs
│   ├── PaymentDomainMetrics.cs
│   └── OperationFeeCalculatorService.cs
├── Gateways/
│   ├── BtcPayServerPaymentService.cs
│   ├── AbacatePayPixService.cs
│   ├── EfiBankPixService.cs
│   └── GatewayService.cs
├── Admin/
│   ├── AdminConfirmationService.cs
│   ├── AdminLogsQueryService.cs
│   └── AdminSettingsService.cs
├── Events/
│   ├── EventNotificationService.cs
│   ├── EventCollisionService.cs
│   └── RachaSchedulerService.cs
├── Webhooks/
│   ├── BtcPayWebhookService.cs
│   ├── AbacatePayWebhookService.cs
│   └── EfiBankWebhookService.cs
└── Shared/
    ├── LogService.cs
    ├── UserService.cs
    └── UiTextService.cs
```

### 7.4 Reduzir StateHasChanged() Overuse

**Ações:**

1. **Audit todas as 101 chamadas:**
   - Manter apenas as necessárias (callbacks externos, síncronos)
   - Remover as desnecessárias (após await)

2. **Implementar padrão de debouncing:**
```csharp
public class DebounceDispatcher
{
    private Dictionary<string, CancellationTokenSource> _pending = new();
    
    public async Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        if (_pending.TryGetValue(key, out var cts))
            cts.Cancel();
        
        var newCts = new CancellationTokenSource();
        _pending[key] = newCts;
        
        try
        {
            await Task.Delay(delay, newCts.Token);
            await action();
        }
        finally
        {
            _pending.Remove(key);
        }
    }
}
```

3. **Usar o DebounceDispatcher existente:**
```csharp
// Em vez de múltiplos StateHasChanged() durante digitação
await _debounce.DebounceAsync("search", async () =>
{
    await LoadSearchResults();
    // Re-render automático
}, TimeSpan.FromMilliseconds(300));
```

### 7.5 Implementar IAsyncDisposable Globalmente

**Auditoria:**
- Páginas com EventHandlers longos
- Páginas com SignalR subscriptions
- Páginas com timers (Task.Delay loops)

**Exemplo:**
```csharp
@implements IAsyncDisposable
@inject IHubContext<PaymentHub> HubContext

private IAsyncDisposable? _hubConnection;

protected override async Task OnInitializedAsync()
{
    _hubConnection = HubContext.Clients.All.SendAsync(...);
}

async ValueTask IAsyncDisposable.DisposeAsync()
{
    if (_hubConnection is not null)
        await _hubConnection.DisposeAsync();
    GC.SuppressFinalize(this);
}
```

### 7.6 Melhorias de Performance

1. **Virtual Scrolling para listas grandes:**
   - AdminPayments com 1000+ registros
   - Use Virtualize<T> do Blazor

2. **Lazy Loading de componentes:**
   - Carregar modals sob demanda
   - Usar @_deferredContent apenas quando necessário

3. **CSS Coverage:**
   - Auditoria com ferramentas (DevTools)
   - Remover estilos não utilizados

### 7.7 Segurança Adicional

1. **Input Validation:**
   - Validar todos os [Parameter]
   - Whitelist de valores permitidos

2. **CORS Configuration:**
   - Verificar se está configurado corretamente
   - API Key rotation policy

3. **Secrets Management:**
   - Migrar de User Secrets para vault (produção)
   - Implementar key rotation

### 7.8 Testes

**Cobertura Atual:**
- 140+ testes em Confirmai.Tests/
- Foco em Admin, Payment, Delinquency flows

**Recomendações:**
1. Aumentar cobertura de componentes
2. Adicionar testes E2E com Playwright
3. Testes de performance (load testing)
4. Testes de segurança (OWASP)

### 7.9 Documentação

1. **README.md ampliado:**
   - Arquitetura de payment gateways
   - Fluxos de autenticação
   - Guia de deployment

2. **Inline documentation:**
   - Adicionar XML comments em serviços públicos
   - Documentar invariantes de negócio

3. **Architecture Decision Records (ADR):**
   - Por que múltiplos gateways?
   - Por que Blazor Server vs WASM?
   - Por que PostgreSQL?

### 7.10 Matriz de Prioridades

| Melhoria | Impacto | Esforço | Prioridade |
|----------|---------|--------|-----------|
| Reduzir StateHasChanged | Alto | Médio | 🔴 **ALTA** |
| Implementar IAsyncDisposable | Alto | Médio | 🔴 **ALTA** |
| Refatorar AdminPayments | Médio | Alto | 🟡 **MÉDIA** |
| Organizar Services em pastas | Médio | Baixo | 🟡 **MÉDIA** |
| Consolidar CSS | Médio | Médio | 🟡 **MÉDIA** |
| Adicionar documentação | Médio | Baixo | 🟡 **MÉDIA** |
| Virtual Scrolling | Baixo | Alto | 🟢 **BAIXA** |
| Key Rotation policy | Médio | Alto | 🟡 **MÉDIA** |

---

## 📋 Recomendações Consolidadas

### Curto Prazo (1-2 semanas)

1. ✅ **Auditoria de StateHasChanged()** - Remover 60%+ das chamadas desnecessárias
2. ✅ **Implementar IAsyncDisposable** - Em todas as páginas com recursos longos
3. ✅ **Documentar Padrões** - Criar guide de boas práticas para o time

### Médio Prazo (1-2 meses)

1. ✅ **Refatorar componentes grandes** - Dividir AdminPayments, Groups/Detail
2. ✅ **Organizar Services** - Criar subpastas temáticas
3. ✅ **Consolidar CSS** - Eliminar duplicação, padronizar tokens

### Longo Prazo (3-6 meses)

1. ✅ **Migrar de User Secrets para Vault** - Produção
2. ✅ **Implementar Virtual Scrolling** - Listas com 1000+ itens
3. ✅ **Aumentar cobertura de testes** - Target: 80%+
4. ✅ **Architecture Review** - Considerar CQRS para domínios críticos

---

## 📊 Conclusão

O projeto **Confirmai** demonstra:

### ✅ **Pontos Fortes**
- Arquitetura limpa com separação de concerns
- Segurança bem implementada (Identity, CSRF, Rate Limiting)
- Padrões de DI consistentes
- Suporte a múltiplos gateways de pagamento
- Testes adequados para fluxos críticos
- CSS organizado com design tokens

### ⚠️ **Áreas de Melhoria**
- Componentes muito grandes (especialment Admin)
- StateHasChanged() overuse (101 chamadas)
- IAsyncDisposable não implementado globalmente
- Serviços desorganizados (71 no nível raiz)
- CSS com duplicação (oportunidade de consolidação)

### 🎯 **Próximos Passos Recomendados**

1. **Sprint 1:** Reduzir StateHasChanged() + Implementar IAsyncDisposable
2. **Sprint 2:** Refatorar componentes grandes + Organizar Services
3. **Sprint 3:** Consolidar CSS + Documentação

---

**Relatório Preparado:** 2 de junho, 2026  
**Versão:** 1.0  
**Status:** ✅ Completo
