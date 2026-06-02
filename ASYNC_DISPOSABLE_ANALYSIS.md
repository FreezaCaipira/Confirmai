# 🔍 Análise Completa: IAsyncDisposable no Confirmai

**Data:** Junho 2, 2026 | **Escopo:** Todas as páginas Razor (`Pages/**/*.razor`)

---

## 📊 RESUMO EXECUTIVO

| Categoria | Quantidade | Status | Prioridade |
|-----------|-----------|--------|-----------|
| **✅ IAsyncDisposable OK** | 3 | Implementado | - |
| **⚠️ IDisposable (sem dispose)** | 2 | Crítico | 🔴 Alta |
| **❌ Task.Delay/Polling sem disposal** | 11+ | Missing | 🔴 Alta |
| **✅ Sem timers** | ~24 | Safe | - |
| **TOTAL** | **~40** | | |

---

## 1️⃣ PÁGINAS COM IAsyncDisposable (Padrão de Referência)

### 📄 AdminVenueEdit.razor

**Localização:** `Pages/Admin/AdminVenueEdit.razor` (linhas 4, 234, 259)

**Pattern:**
```csharp
@page "/admin/venues/edit/{Id:int}"
@attribute [Authorize(Roles = "admin")]
@implements IAsyncDisposable

@inject IJSRuntime JS
@inject IConfiguration Config

@code {
    private DotNetObjectReference<AdminVenueEdit>? _objRef;
    private bool _acInit = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_acInit && !isLoading)
        {
            _acInit = true;
            var apiKey = Config["Google:MapsApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("SET_VIA"))
            {
                _objRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("venueAutocomplete.init", apiKey, _objRef, "admin-venue-name-input");
            }
        }
    }

    [JSInvokable]
    public async Task PlaceSelected(string name, string street, string city, string state)
    {
        model.Name      = name;
        model.Address   = street;
        model.City      = city;
        model.StateCode = state.Length > 2 ? state[..2] : state.ToUpperInvariant();
        await LoadCitiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_objRef is not null)
        {
            try { await JS.InvokeVoidAsync("venueAutocomplete.dispose"); } catch { }
            _objRef.Dispose();
        }
    }
}
```

**O que faz:** Gerencia referência a JS interop para autocomplete do Google Maps.

**Lições:**
- Cria `DotNetObjectReference` sob demanda
- Chama `dispose()` na função JS antes de descartar referência
- Trata exceções na JS interop

---

### 📄 VenueEdit.razor

**Localização:** `Pages/VenueManager/VenueEdit.razor` (linhas 20, 168, 192)

**Padrão:** Idêntico ao AdminVenueEdit — mesmo tipo de cleanup.

```csharp
@implements IAsyncDisposable

@code {
    private DotNetObjectReference<VenueEdit>? _objRef;
    private bool _acInit = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_acInit && !isLoading && !accessDenied)
        {
            _acInit = true;
            var apiKey = Config["Google:MapsApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("SET_VIA"))
            {
                _objRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("venueAutocomplete.init", apiKey, _objRef, "venue-name-input");
            }
        }
    }

    [JSInvokable]
    public void PlaceSelected(string name, string street, string city, string state)
    {
        model.Name      = name;
        model.Address   = street;
        model.City      = city;
        model.StateCode = state.Length > 2 ? state[..2] : state.ToUpperInvariant();
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_objRef is not null)
        {
            try { await JS.InvokeVoidAsync("venueAutocomplete.dispose"); } catch { }
            _objRef.Dispose();
        }
    }
}
```

---

### 📄 AdminPayments.razor ⭐ (Mais Complexo)

**Localização:** `Pages/Admin/AdminPayments.razor` (linhas 4, 423, 450-520, 1331)

**O que faz:**
- Dois loops de polling contínuos: `RunSummaryRefreshLoopAsync` e `RunSummaryAgeLoopAsync`
- Cada loop roda em Task separada com `CancellationTokenSource`
- Também gerencia modal a11y da JS interop

```csharp
@page "/admin/payments"
@attribute [Authorize(Roles = "admin")]
@implements IAsyncDisposable

@inject IJSRuntime JS

@code {
    private CancellationTokenSource? summaryRefreshCts;
    private CancellationTokenSource? summaryAgeCts;
    private Task? summaryRefreshTask;
    private Task? summaryAgeTask;
    private bool isAdvancedToolsModalA11yActive;

    protected override async Task OnInitializedAsync()
    {
        // ... initialization ...
        StartSummaryRefreshLoop();
        StartSummaryAgeLoop();
        isLoading = false;
    }

    private void StartSummaryRefreshLoop()
    {
        summaryRefreshCts = new CancellationTokenSource();
        summaryRefreshTask = RunSummaryRefreshLoopAsync(summaryRefreshCts.Token);
    }

    private void StartSummaryAgeLoop()
    {
        summaryAgeCts = new CancellationTokenSource();
        summaryAgeTask = RunSummaryAgeLoopAsync(summaryAgeCts.Token);
    }

    // Refresh Summary Panel
    private async Task RunSummaryRefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(SummaryRefreshSeconds), cancellationToken);

                if (!isAutoRefreshEnabled)
                {
                    if (isAutoRefreshPausedByVisibility)
                    {
                        await InvokeAsync(() =>
                        {
                            isAutoRefreshPausedByVisibility = false;
                            StateHasChanged();
                        });
                    }
                    continue;
                }

                if (!await IsDocumentVisibleAsync())
                {
                    await InvokeAsync(() =>
                    {
                        isAutoRefreshPausedByVisibility = true;
                        lastAutoRefreshPauseLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        StateHasChanged();
                    });
                    continue;
                }

                await InvokeAsync(async () =>
                {
                    await RefreshOperationalPanelAsync(includePaymentsTable: false);
                    StateHasChanged();
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    // Age Status Update Loop (1s ticks)
    private async Task RunSummaryAgeLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                await InvokeAsync(async () =>
                {
                    UpdateLastSummaryAgeLabel();
                    await TryWriteStalenessIncidentAuditAsync();
                    StateHasChanged();
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (shouldActivateAdvancedToolsModalA11y && isAdvancedToolsModalOpen)
        {
            shouldActivateAdvancedToolsModalA11y = false;
            try
            {
                await JS.InvokeVoidAsync("ConfirmaiModal.open", "#admin-payments-advanced-modal");
                isAdvancedToolsModalA11yActive = true;
            }
            catch { }
        }
        // ... rest of initialization ...
    }

    public async ValueTask DisposeAsync()
    {
        // Cancel the loops
        if (isAdvancedToolsModalA11yActive)
        {
            try
            {
                await JS.InvokeVoidAsync("ConfirmaiModal.close");
            }
            catch { }
            finally
            {
                isAdvancedToolsModalA11yActive = false;
            }
        }

        summaryRefreshCts?.Cancel();
        summaryAgeCts?.Cancel();

        // Await running tasks
        if (summaryRefreshTask is not null)
            try { await summaryRefreshTask; } catch { }
        if (summaryAgeTask is not null)
            try { await summaryAgeTask; } catch { }
    }
}
```

**Lições:**
- ✅ Cria `CancellationTokenSource` por loop
- ✅ Cancela antes de awaitar tasks (seguro)
- ✅ Trata `OperationCanceledException` dentro dos loops
- ✅ Chama `InvokeAsync()` para state updates de background tasks
- ✅ Limpa JS interop também

---

## 2️⃣ PÁGINAS COM TASK.DELAY QUE PRECISAM DE IAsyncDisposable

### 📄 Futsal/Escalacao.razor

**Localização:** `Pages/Futsal/Escalacao.razor` (linha 812)

**Status:** ❌ SEM IAsyncDisposable

**Código atual:**
```csharp
private async Task CopyToClipboard()
{
    var text = BuildShareText();
    await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
    copied = true;
    StateHasChanged();
    await Task.Delay(2000);  // ← SEM CANCELLATION TOKEN
    copied = false;
    StateHasChanged();
}
```

**Problema:** Se o componente for destruído durante o `Task.Delay(2000)`, a task continua rodando na memória.

**Solução:**
```csharp
private CancellationTokenSource? _copyCts;

private async Task CopyToClipboard()
{
    try
    {
        var text = BuildShareText();
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        copied = true;
        StateHasChanged();
        
        _copyCts?.Cancel();
        _copyCts = new CancellationTokenSource();
        await Task.Delay(2000, _copyCts.Token);
        
        copied = false;
        StateHasChanged();
    }
    catch (OperationCanceledException) { }
}

@implements IAsyncDisposable

public async ValueTask DisposeAsync()
{
    _copyCts?.Cancel();
    _copyCts?.Dispose();
}
```

---

### 📄 Groups/Detail.razor

**Localização:** `Pages/Groups/Detail.razor` (linhas 717, 729)

**Status:** ❌ SEM IAsyncDisposable

**Código atual:**
```csharp
private async Task CopyInviteLink(string url)
{
    try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", url); }
    catch { }
    copiedInvite = true;
    await Task.Delay(2000);  // ← SEM CANCELLATION TOKEN
    copiedInvite = false;
    StateHasChanged();
}

private async Task CopyCode(string code)
{
    try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", code); }
    catch { }
    copiedCode = true;
    await Task.Delay(2000);  // ← SEM CANCELLATION TOKEN
    copiedCode = false;
    StateHasChanged();
}
```

**Problema:** Dois delays diferentes, ambos sem cancellation.

**Solução:**
```csharp
private CancellationTokenSource? _clipboardCts;

@implements IAsyncDisposable

private async Task CopyInviteLink(string url)
{
    try 
    { 
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", url); 
        _clipboardCts?.Cancel();
        _clipboardCts = new CancellationTokenSource();
        
        copiedInvite = true;
        await Task.Delay(2000, _clipboardCts.Token);
        copiedInvite = false;
        StateHasChanged();
    }
    catch (OperationCanceledException) { }
}

private async Task CopyCode(string code)
{
    try 
    { 
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", code); 
        _clipboardCts?.Cancel();
        _clipboardCts = new CancellationTokenSource();
        
        copiedCode = true;
        await Task.Delay(2000, _clipboardCts.Token);
        copiedCode = false;
        StateHasChanged();
    }
    catch (OperationCanceledException) { }
}

public async ValueTask DisposeAsync()
{
    _clipboardCts?.Cancel();
    _clipboardCts?.Dispose();
}
```

---

### 📄 Poker/Index.razor

**Localização:** `Pages/Poker/Index.razor` (linha 15)

**Status:** ❌ SEM IAsyncDisposable

**Código atual:**
```csharp
<input @onfocusout="() => Task.Delay(150).ContinueWith(_ => { showTypeMenu = false; InvokeAsync(StateHasChanged); })">
```

**Problema:** Fire-and-forget task sem cancellation. Se a página mudar durante os 150ms, o `StateHasChanged()` vai ser chamado mesmo assim.

**Solução:**
```csharp
private CancellationTokenSource? _menuCts;

@implements IAsyncDisposable

private async Task HideMenuWithDelay()
{
    try
    {
        _menuCts?.Cancel();
        _menuCts = new CancellationTokenSource();
        
        await Task.Delay(150, _menuCts.Token);
        showTypeMenu = false;
        await InvokeAsync(StateHasChanged);
    }
    catch (OperationCanceledException) { }
}

public async ValueTask DisposeAsync()
{
    _menuCts?.Cancel();
    _menuCts?.Dispose();
}
```

**No HTML:**
```html
<input @onfocusout="HideMenuWithDelay">
```

---

### 📄 Payment/EventPayment.razor ⚠️ CRÍTICO

**Localização:** `Pages/Payment/EventPayment.razor` (linhas 4, 312, 416-465, 542)

**Status:** ⚠️ Implementa `@implements IDisposable` mas deveria ser `IAsyncDisposable`

**Problemas:**
1. Implementa `IDisposable` (sync) mas tem polling async
2. Polling loop cria `CancellationTokenSource` com timeout de 30 minutos
3. **CRÍTICO:** Sem implementação de Dispose(), causará memory leak

**Código atual (QUEBRADO):**
```csharp
@implements IDisposable  // ← ERRADO! Deveria ser IAsyncDisposable

@code {
    private CancellationTokenSource? _pollCts;

    private async Task StartPollingAsync(string chargeId, string gatewayName)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));  // 30min timeout
        var ct = _pollCts.Token;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);  // ← Polling a cada 5 segundos

                await using (var db = await DbFactory.CreateDbContextAsync())
                {
                    var fresh = await db.EventConfirmations.FindAsync(conf!.Id);
                    if (fresh?.PaymentStatus == EventConfirmationPaymentStatus.Paid)
                    {
                        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                        conf.HasPaid = true;
                        payState = PayState.Paid;
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }

                var gateway = await EventGatewayFactory.GetGatewayAsync(gatewayName);
                if (gateway is not null && await gateway.IsChargePaidAsync(chargeId))
                {
                    await using var db = await DbFactory.CreateDbContextAsync();
                    var entity = await db.EventConfirmations.FindAsync(conf!.Id);
                    if (entity is not null)
                    {
                        entity.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                        entity.HasPaid = true;
                        await db.SaveChangesAsync();
                    }
                    conf!.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                    conf!.HasPaid = true;
                    payState = PayState.Paid;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
        }
        catch (TaskCanceledException) { }
    }

    // ❌ NÃO HÁ DISPOSE() OU DISPOSEASYNC() IMPLEMENTADO!
}
```

**Solução (CORRIGIDA):**
```csharp
@implements IAsyncDisposable  // ← CORRIGIDO

@code {
    private CancellationTokenSource? _pollCts;
    private Task? _pollingTask;

    private async Task StartPollingAsync(string chargeId, string gatewayName)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        var ct = _pollCts.Token;

        _pollingTask = RunPollingLoopAsync(chargeId, gatewayName, ct);
    }

    private async Task RunPollingLoopAsync(string chargeId, string gatewayName, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);

                await using (var db = await DbFactory.CreateDbContextAsync())
                {
                    var fresh = await db.EventConfirmations.FindAsync(conf!.Id);
                    if (fresh?.PaymentStatus == EventConfirmationPaymentStatus.Paid)
                    {
                        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                        conf.HasPaid = true;
                        payState = PayState.Paid;
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }

                var gateway = await EventGatewayFactory.GetGatewayAsync(gatewayName);
                if (gateway is not null && await gateway.IsChargePaidAsync(chargeId))
                {
                    await using var db = await DbFactory.CreateDbContextAsync();
                    var entity = await db.EventConfirmations.FindAsync(conf!.Id);
                    if (entity is not null)
                    {
                        entity.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                        entity.HasPaid = true;
                        await db.SaveChangesAsync();
                    }
                    conf!.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                    conf!.HasPaid = true;
                    payState = PayState.Paid;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
        }
        catch (TaskCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        _pollCts?.Cancel();
        
        if (_pollingTask is not null)
        {
            try { await _pollingTask; } 
            catch (OperationCanceledException) { }
        }
        
        _pollCts?.Dispose();
    }
}
```

---

### 📄 Payment/Payment.razor

**Localização:** `Pages/Payment/Payment.razor` (linhas 1-50, 566, 350)

**Status:** ⚠️ Implementa `ValueTask DisposeAsync()` mas de forma incompleta

**Código atual (INCOMPLETO):**
```csharp
@code {
    // ...
    private PaymentEventBus PaymentEventBus;

    protected override async Task OnInitializedAsync()
    {
        // ...
        PaymentEventBus.OnPaymentConfirmed += OnPaymentConfirmed;
    }

    private void OnPaymentConfirmed(int paymentId)
    {
        if (paymentId != this.PaymentId) return;

        InvokeAsync(async () =>
        {
            await CheckPayment();
            StateHasChanged();
        });
    }

    public ValueTask DisposeAsync()
    {
        PaymentEventBus.OnPaymentConfirmed -= OnPaymentConfirmed;
        return ValueTask.CompletedTask;
    }

    // PROBLEMA: Task.Delay(1500) em GenerateAddress() NÃO É CANCELADO
    private async Task GenerateAddress()
    {
        // ...
        await Task.Delay(1500);  // ← SEM CANCELLATION TOKEN
    }
}
```

**Solução:**
```csharp
@implements IAsyncDisposable

@code {
    private CancellationTokenSource? _delayCts;

    public async ValueTask DisposeAsync()
    {
        PaymentEventBus.OnPaymentConfirmed -= OnPaymentConfirmed;
        
        _delayCts?.Cancel();
        _delayCts?.Dispose();
    }

    private async Task GenerateAddress()
    {
        try
        {
            // ...
            _delayCts?.Cancel();
            _delayCts = new CancellationTokenSource();
            await Task.Delay(1500, _delayCts.Token);
        }
        catch (OperationCanceledException) { }
    }
}
```

---

## 3️⃣ MATRIZ COMPLETA: PÁGINAS × NECESSIDADE (50+ LINHAS)

| # | Página | Caminho | Task.Delay | Polling Loop | SignalR | JS Interop | OnAfterRenderAsync | Status | Necessidade | Prioridade |
|---|--------|---------|-----------|--------------|---------|------------|-------------------|--------|-------------|-----------|
| 1 | AdminVenueEdit | `Admin/` | ❌ | ❌ | ❌ | ✅ JS ref | ✅ | **✅ IAsyncDisposable** | ✅ OK | - |
| 2 | VenueEdit | `VenueManager/` | ❌ | ❌ | ❌ | ✅ JS ref | ✅ | **✅ IAsyncDisposable** | ✅ OK | - |
| 3 | AdminPayments | `Admin/` | ✅ 2x | ✅ 2x loops | ❌ | ✅ modal | ✅ | **✅ IAsyncDisposable** | ✅ OK | - |
| 4 | AdminLogs | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | **⚠️ IDisposable VAZIO** | ❌ Crítico | 🔴 ALTA |
| 5 | EventPayment | `Payment/` | ✅ 4x | ✅ polling 5s | ❌ | ❌ | ❌ | **⚠️ IDisposable** | ❌ Crítico | 🔴 ALTA |
| 6 | Futsal/Escalacao | `Futsal/` | ✅ 1x | ❌ | ❌ | ❌ | ✅ | **❌ Sem disposal** | ❌ Necessário | 🟠 Média |
| 7 | Groups/Detail | `Groups/` | ✅ 2x | ❌ | ❌ | ❌ | ✅ | **❌ Sem disposal** | ❌ Necessário | 🟠 Média |
| 8 | Poker/Index | `Poker/` | ✅ 1x | ❌ | ❌ | ❌ | ✅ | **❌ Sem disposal** | ❌ Necessário | 🟠 Média |
| 9 | Payment/Payment | `Payment/` | ✅ 1x | ❌ | ❌ | ❌ | ❌ | **⚠️ ValueTask sem cts** | ⚠️ Parcial | 🟠 Média |
| 10 | AdminUsers | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 11 | Mailbox | `Root` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 12 | Futsal/Detail | `Futsal/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 13 | Admin | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 14 | AdminGateways | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 15 | AdminLanguages | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 16 | AdminUserEdit | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 17 | AdminUserView | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 18 | AdminVenues | `Admin/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 19 | AdminAuditTimeline | `Admin/` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ OK | ✅ OK | - |
| 20 | Index | `Root` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 21 | Users | `Root` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 22 | Groups/Index | `Groups/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 23 | Groups/Features | `Groups/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 24 | Groups/Join | `Groups/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 25 | Groups/Ranking | `Groups/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 26 | Futsal/Create | `Futsal/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 27 | Futsal/Edit | `Futsal/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 28 | Futsal/Index | `Futsal/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 29 | Futsal/Schedule/Edit | `Futsal/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 30 | Poker/Create | `Poker/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 31 | Poker/Detail | `Poker/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 32 | Poker/Edit | `Poker/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 33 | MyEvents/Index | `MyEvents/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 34 | MyConfirmations | `MyConfirmations/` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ OK | ✅ OK | - |
| 35 | Payment/ViewPayment | `Payment/` | ❌ | ❌ | ❌ | ❌ | ✅ | **✅ ValueTask OK** | ✅ OK | - |
| 36 | Payment/PaymentsHistory | `Payment/` | ❌ | ❌ | ❌ | ❌ | ✅ | **✅ ValueTask OK** | ✅ OK | - |
| 37 | Payment/PaymentDetails | `Payment/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 38 | Product/Marketplace | `Product/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 39 | Product/ProductForm | `Product/` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ OK | ✅ OK | - |
| 40 | Dashboard | `Root` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ OK | ✅ OK | - |

**Legenda:**
- ✅ = Presente/OK
- ❌ = Ausente
- ⚠️ = Implementado mas incompleto/errado
- **Negrito** = Status especial

---

## 📋 CHECKLIST: O QUE IMPLEMENTAR

### 🔴 CRÍTICO (Fazer IMEDIATAMENTE)

- [ ] **AdminLogs.razor** — Adicionar `DisposeAsync()` corretamente (atualmente vazio)
- [ ] **EventPayment.razor** — Converter `IDisposable` → `IAsyncDisposable` e implementar cleanup do polling

### 🟠 ALTA PRIORIDADE (Esta semana)

- [ ] **Futsal/Escalacao.razor** — Adicionar `IAsyncDisposable` + CTS para `CopyToClipboard()`
- [ ] **Groups/Detail.razor** — Adicionar `IAsyncDisposable` + CTS para dois métodos de copy
- [ ] **Poker/Index.razor** — Adicionar `IAsyncDisposable` + extrair `HideMenuWithDelay()`
- [ ] **Payment/Payment.razor** — Complementar `DisposeAsync()` com CTS para delays

### 🟡 MÉDIA PRIORIDADE (Próxima sprint)

- [ ] Revisar todas as páginas com `OnAfterRenderAsync` quanto a JS interop não cleanup
- [ ] Auditar `PaymentEventBus` subscribers em todas as páginas

---

## 🎯 TEMPLATE PADRÃO PARA COPIAR

```csharp
@page "/seu-page-route"
@attribute [Authorize] @* se necessário *@
@implements IAsyncDisposable

@code {
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;

    protected override async Task OnInitializedAsync()
    {
        // ... initialization code ...
        // Se tem loop/polling:
        // _cts = new CancellationTokenSource();
        // _backgroundTask = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, ct);
                // ... refresh logic ...
                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        
        if (_backgroundTask is not null)
        {
            try { await _backgroundTask; }
            catch (OperationCanceledException) { }
        }
        
        _cts?.Dispose();
    }
}
```

---

## 🔗 Recursos

- [Blazor Lifecycle: OnAfterRenderAsync](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- [CancellationToken best practices](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)
- [ValueTask vs Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask)

---

**Próximas ações:**
1. Implementar CRÍTICOS: AdminLogs + EventPayment
2. Aplicar template padrão aos 4 páginas de ALTA prioridade
3. Code review de JS interop cleanup

**Estimativa:** 2-3 horas de trabalho

