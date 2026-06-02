# 📋 Revisão Estrutural - Progresso e Próximos Passos

**Data de Início:** 2 de junho, 2026  
**Status:** 🔄 Em andamento - Fase 1 (IAsyncDisposable)

---

## ✅ Concluído

### 1. Análise Completa
- ✅ Exploração estrutural CSS, Blazor, Serviços, Segurança
- ✅ Identificação de 5 problemas críticos/médios
- ✅ Geração de relatórios:
  - `PROJECT_ANALYSIS.md` (15.000+ palavras)
  - `CODE_PATTERNS_ANALYSIS.md` (8.000+ palavras)
- ✅ Atualização de README com achados e plano

**Commits:**
- Análise estrutural documentada
- README atualizado com Revisão Estrutural

---

## 🔄 Em Progresso

### Fase 1: IAsyncDisposable (CRÍTICO)
**Objetivo:** Implementar `IAsyncDisposable` em todas as páginas para prevenir memory leaks

**Status:** Iniciando...

**Escopo:**
- [ ] Páginas que usam `Task.Delay` loops (delinquency notifications, etc.)
- [ ] Páginas com SignalR subscriptions
- [ ] Páginas com timers ou polling
- [ ] Análise de quais 3 páginas já têm implementação (como referência)

**Ferramentas:**
```csharp
public async ValueTask DisposeAsync()
{
    _cts?.Cancel();
    _cts?.Dispose();
    // Unsubscribe de eventos
    // Liberar recursos
}
```

**Referências:**
- Blazor Docs: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-9.0#handle-incomplete-async-operations-at-render-or-disposal
- Current investigation: Páginas com `await Task.Delay(...)` sem cancelamento

---

## 📋 Backlog Priorizado

### Fase 2: StateHasChanged Audit (CRÍTICO)
**Objective:** Remover 60+ chamadas desnecessárias de `StateHasChanged()`  
**Estimativa:** 1-2 semanas  
**Impacto:** Reduz re-renders em toda a aplicação, melhora performance  

**Padrões a buscar:**
- `StateHasChanged()` após `await` sem mudança de estado
- `StateHasChanged()` em MainLayout (afeta toda página)
- `StateHasChanged()` em handlers de eventos Blazor (redundante)

### Fase 3: Refatorar Componentes Grandes (MÉDIA)
**Escopo:**
- AdminPayments.razor (1.220 linhas) → 3-4 componentes
- Groups/Detail.razor (1.041 linhas) → componentes
- Futsal/Escalacao.razor (complexidade alta)

**Objetivo:** Melhorar testabilidade, manutenibilidade, reutilização

**Estimativa:** 1-2 meses

### Fase 4: Reorganizar Serviços (MÉDIA)
**Objetivo:** 71+ serviços em subpastas temáticas

**Estrutura proposta:**
```
Services/
├── Admin/
│   ├── AdminLogsService.cs
│   ├── AdminPaymentsService.cs
│   ├── AdminSecurityPolicyService.cs
│   └── ...
├── Payment/
│   ├── PaymentConfirmationService.cs
│   ├── PixProofUploadService.cs
│   └── ...
├── Events/
│   ├── EventConfirmationService.cs
│   ├── EventCollisionService.cs
│   └── ...
├── Groups/
├── User/
├── Notifications/
├── Webhook/
└── ...
```

**Estimativa:** 2-3 semanas

### Fase 5: CSS Consolidation (MÉDIA)
**Objetivo:** Extrair padrões duplicados em utilities reutilizáveis

**Exemplos:**
- `.card` — classe base reutilizável para cards
- `.gradient--futsal`, `.gradient--poker` — gradientes por esporte
- `.player-tag` — namespace consolidado para badges
- `.btn-*` — sistema de botões unificado

**Estimativa:** 1-2 semanas

### Fase 6: Virtual Scrolling (BAIXA)
**Escopo:** Implementar em listas grandes (Admin/Logs, etc.)  
**Ferramentas:** Blazor Virtual Component ou terceiros  
**Estimativa:** 3-4 semanas

### Fase 7: Test Coverage → 80% (BAIXA)
**Status:** Atual: 421+ testes passando  
**Objetivo:** Cobertura de 80%+  
**Estimativa:** 1-2 meses

---

## 🔗 Referências

**Documentação Gerada:**
- [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md) — análise estrutural completa
- [CODE_PATTERNS_ANALYSIS.md](CODE_PATTERNS_ANALYSIS.md) — padrões e anti-patterns específicos
- [README.md](README.md) — atualizado com plano de ação

**Recursos Blazor:**
- [Lifecycle Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-9.0)
- [IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable?view=net-9.0)
- [Performance Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-9.0)

---

## 📝 Notas de Desenvolvimento

### Task.Delay Loops Encontrados
- Delinquency notifications: polling periódico
- Payment reconciliation: verificação de status
- Necessário converter para `IAsyncDisposable` com `CancellationToken`

### StateHasChanged() Hotspots
- MainLayout: re-renderiza componentes não afetados
- AdminPayments: chamadas em loops dentro de `@foreach`
- Payment pages: após `await` sem mudança de estado

### CSS Duplicado Identificado
- `.player-tag*` — 10+ variações de badges (consolid para namespace único)
- `.card` — ~8 implementações diferentes
- `.gradient*` — gradientes repetidos por esporte/tipo

---

## Métricas de Sucesso

| Métrica | Baseline | Meta |
|---------|----------|------|
| Chamadas de StateHasChanged | 101 | ≤ 40 (-60%) |
| Páginas com IAsyncDisposable | 3 | 50+ (-100%) |
| Componentes > 500 linhas | 10+ | ≤ 5 |
| Linhas de CSS | 15.000+ | ≤ 10.000 (-30%) |
| Serviços em root | 71 | 10-15 |
| Cobertura de testes | 60%* | 80%+ |

*Estimado com base em 421+ testes

---

## Contato & Perguntas

Para dúvidas sobre a revisão ou próximos passos, consulte:
- [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md) — análise detalhada
- [DEVELOPMENT.md](DEVELOPMENT.md) — contexto técnico
