# Plano de Transição Híbrido — Confirmai

> Gerado em 14/06/2026 | Atualizado em 22/06/2026 | Base: `main`
> 641/641 testes passando | 0 erros de build | 0 warnings CS1998/CS0649/CS86xx

---

## Estado Atual — O que já foi feito

- [x] Documentação consolidada (22 .md → 3 centrais)
- [x] 24 testes corrigidos (575→641 total)
- [x] Migration pendente gerada (ServerApiKeys)
- [x] Services auditados e limpos (namespaces, GatewayService, file-scoped)
- [x] CONTRIBUTING.md verificado contra código real
- [x] L1: Eliminar ~20 inline styles estáticos (Groups/Join, Groups/Index, Groups/Detail, Index, Poker/Edit)
- [x] L2: Criar `.razor.css` para todas as páginas (110+ arquivos)
- [x] L3: Corrigir 40 warnings CS1998 (async sem await)
- [x] L4: Corrigir 18 warnings CS0649 (campos nunca atribuídos)
- [x] L5: Corrigir warnings CS8618/CS8602/CS8604/CS8601 (nullability)

---

## 🖥️ MODO LOCAL (Cascade / IDE) — Tarefas Mecânicas e Seguras

Essas tarefas são **isoladas por arquivo**, não exigem contexto cross-project, e podem ser verificadas com `dotnet build` + `dotnet test` a cada passo.

### L1. Eliminar inline styles estáticos (~20 ocorrências)

**Onde**: `Pages/Groups/Join.razor`, `Pages/Groups/Index.razor`, `Pages/Groups/Detail.razor`, `Pages/Index.razor`

**Como fazer**:
```bash
# Achar todos os inline styles
grep -rn 'style="' Pages/Groups/ Pages/Index.razor --include="*.razor"
```

**Padrão**: Para cada `style="..."` estático encontrado:
1. Criar classe equivalente no `.razor.css` do componente (criar o arquivo se não existir)
2. Substituir `style="color: red"` por `class="minha-classe"`
3. Rodar `dotnet build` para verificar

**Validação**: `dotnet build` sem erros + visual no browser (`./dev.ps1`)

---

### L2. Criar `.razor.css` para páginas que não têm (54 faltam)

**Prioridade**: Começar pelas 16 páginas com >400 linhas.

**Como listar as que faltam**:
```bash
# PowerShell (Windows)
Get-ChildItem -Recurse Pages/*.razor | Where-Object { -not (Test-Path "$($_.FullName).css") } | Select Name

# Bash
for f in $(find Pages/ -name "*.razor" -not -name "_*"); do [ ! -f "${f}.css" ] && echo $f; done
```

**Padrão**: Criar arquivo vazio `NomeDaPagina.razor.css` ao lado do `.razor`. Se a página tem estilos inline, migrar para o CSS. Se não tem, pode criar vazio e popular depois.

**Validação**: `dotnet build` (Blazor detecta `.razor.css` automaticamente)

---

### L3. Corrigir warnings CS1998 (async sem await) — 40 ocorrências

**Onde**: Vários arquivos em `Pages/`

**Como encontrar**:
```bash
dotnet build --no-restore 2>&1 | grep "CS1998"
```

**Padrão**: Para cada método `async` sem `await`:
- Se o método não precisa ser async → remover `async` e retornar `Task.CompletedTask`
- Se o método vai precisar de async no futuro → adicionar `await Task.CompletedTask;` (menos ideal)

**Exemplo**:
```csharp
// ANTES
private async Task OnClick() { Navigation.NavigateTo("/"); }

// DEPOIS
private Task OnClick() { Navigation.NavigateTo("/"); return Task.CompletedTask; }
```

**Validação**: `dotnet build` com menos warnings

---

### L4. Corrigir warnings CS0649 (campo nunca atribuído) — 18 ocorrências

**Como encontrar**:
```bash
dotnet build --no-restore 2>&1 | grep "CS0649"
```

**Padrão**: Campos declarados mas nunca usados. Geralmente são para JS interop que ainda não foi implementado. Opções:
- Se o campo é realmente usado via JS → adicionar `= default!;` ou `= null!;`
- Se é dead code → remover o campo

---

### L5. Corrigir warnings CS8618/CS8602/CS8604 (nullability) — 24 ocorrências

**Como encontrar**:
```bash
dotnet build --no-restore 2>&1 | grep "CS8618\|CS8602\|CS8604"
```

**Padrão**:
- `CS8618` (non-nullable não inicializado): adicionar `= default!;` ou `= string.Empty;` ou tornar nullable
- `CS8602` (possível null deref): adicionar null check ou `?.`
- `CS8604` (argumento possivelmente null): adicionar null check antes da chamada

---

## ☁️ MODO CLOUD (Devin) — Tarefas Complexas / Cross-Cutting

Essas tarefas envolvem **múltiplos arquivos interdependentes**, criação de novos componentes com wiring de DI, parâmetros, e testes. Melhor deixar para a nuvem com contexto completo.

### C1. P1 — Decomposição de Componentes Grandes (PRIORIDADE MÁXIMA)

Cada página abaixo precisa ser quebrada em sub-componentes com `[Parameter]`, eventos, e `.razor.css` próprio. Exige entender o fluxo de dados da página inteira.

| Página | Linhas | Já tem Components/ |
|--------|--------|--------------------|
| `Groups/Detail.razor` | 947 | Sim (parcial) |
| `Futsal/Detail.razor` | 933 | Sim (parcial) |
| `AdminLogs.razor` | 695 | Não |
| `Mailbox.razor` | 674 | Sim (parcial) |
| `Poker/Detail.razor` | 628 | Não |
| `Futsal/Escalacao.razor` | 660 | Não |
| `Payment/Payment.razor` | 619 | Não |
| `Poker/Create.razor` | 608 | Não |

**Por que Cloud**: Cada decomposição envolve:
- Análise do grafo de dependências (state, injects, callbacks)
- Criação de componentes com parâmetros tipados
- Criação de `_Imports.razor` se necessário
- Atualização de testes existentes
- Verificação de 641 testes após cada mudança

### C2. DbContext → IDbContextFactory em 9 Services

**9 services** injetam `AppDbContext` diretamente e são usados de Blazor pages (viola padrão do CONTRIBUTING.md):

```
Services/Core/LogService.cs              (56 refs em pages!)
Services/Admin/AdminSettingsService.cs   (17 refs)
Services/Utility/ProductService.cs       (4 refs)
Services/Payment/PaymentConfirmationService.cs (4 refs)
Services/Admin/AdminSecurityPolicyService.cs   (4 refs)
Services/Events/DashboardMetricsService.cs     (2 refs)
Services/Core/AppInitializationService.cs      (0 refs em pages, ok)
Services/Payment/Shared/WebhookPaymentMarker.cs (0 refs em pages, ok)
Services/Payment/BtcPayWebhookService.cs       (0 refs em pages, ok)
```

**Por que Cloud**: `LogService` sozinho tem 56 referências. A migração precisa:
- Mudar construtor de cada service
- Criar `using var db = await _dbFactory.CreateDbContextAsync()` em cada método
- Ajustar testes que montam os services
- Rodar 641 testes após cada service migrado

### C3. UiTextService Phase 2 — Completar EN-US/ES-ES

Completar traduções parciais nos 6 satellite files. Exige verificar cada chave contra o uso real no código.

### C4. Atualizar ROADMAP.md e CONTRIBUTING.md

Após cada batch de mudanças, atualizar docs para refletir estado atual.

---

## 📋 Ordem Sugerida de Execução

### Fase 1 — Local (você faz agora)
```
[ ] L1. Eliminar inline styles (20 ocorrências)
[ ] L2. Criar .razor.css para as 16 páginas >400L que não têm
[ ] L3. Corrigir warnings CS1998 (40 ocorrências)
[ ] L4. Corrigir warnings CS0649 (18 ocorrências)
[ ] L5. Corrigir warnings CS8618/CS8602/CS8604 (24 ocorrências)
```

**Comando de validação após cada tarefa**:
```powershell
dotnet build 2>&1 | Select-String "warning|error" | Group-Object | Sort-Object Count -Descending
dotnet test
```

### Fase 2 — Cloud (Devin na próxima cota)
```
[x] C1. Decompor Groups/Detail.razor (947L)
[x] C1. Decompor Futsal/Detail.razor (933L)
[x] C1. Decompor AdminLogs.razor (695L)
[x] C1. Decompor Mailbox.razor (674L)
[x] C1. Decompor Poker/Detail.razor (628L)
[x] C2. Migrar LogService → IDbContextFactory
[x] C2. Migrar AdminSettingsService → IDbContextFactory
[x] C2. Migrar ProductService → IDbContextFactory
[x] C2. Migrar PaymentConfirmationService → IDbContextFactory
[x] C2. Migrar AdminSecurityPolicyService → IDbContextFactory
[x] C2. Migrar DashboardMetricsService → IDbContextFactory
[x] C3. UiTextService Phase 2 (EN-US/ES-ES)
[ ] C4. Atualizar docs
```

---

## Comandos Úteis

```powershell
# Build com contagem de warnings
dotnet build 2>&1 | Select-String "warning" | Measure-Object

# Testes
dotnet test

# Achar inline styles
Select-String -Path Pages/**/*.razor -Pattern 'style="' -Recurse

# Páginas sem .razor.css
Get-ChildItem -Recurse Pages/*.razor | Where-Object { -not (Test-Path "$($_.FullName).css") }

# Warnings por categoria
dotnet build 2>&1 | Select-String "warning CS" | ForEach-Object { ($_ -split "warning ")[1] -replace ":.*","" } | Group-Object | Sort-Object Count -Descending
```

---

## Regras Importantes (ler antes de começar)

1. **Nunca modificar testes** para fazê-los passar — corrigir o código fonte
2. **Nunca injetar `AppDbContext` diretamente** em Blazor pages — sempre `IDbContextFactory`
3. **Constantes de audit** (`AuditEvents.*`, `AuditEntities.*`) — **nunca renomear**, só adicionar
4. **PageTitle**: toda página precisa de `<PageTitle>Nome · Confirmai</PageTitle>`
5. **CSS vars**: usar os existentes (`--bg-deep`, `--parchment`, `--accent-gold`, etc.) — ver `wwwroot/css/`
6. **Commits**: fazer commits pequenos e frequentes com mensagens descritivas
7. **Branch**: criar branch separada para cada grupo de tarefas (ex: `fix/inline-styles`, `fix/cs-warnings`)
