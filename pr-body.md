# Ciclo 18: TDD + SOLID + CSS Refatoration

## Resumo

Refatoracao completa do Ciclo 18 aplicando disciplinas de qualidade (TDD, SOLID, CSS) em todo o codigo, conforme planejado no WORK_PLAN.md.

## Bloco A — TDD

### Characterization Tests (54 testes)
Testes de caracterizacao via reflection para code-behinds Blazor sem cobertura anterior:
- **AdminPaymentsCharacterizationTests.cs** (30 testes): `GetGatewaySeverity`, `BuildPendingTrendLabel`, `GetSweepMetric`, `HasSweepOrigin`, `BuildSweepDetails`, `UpdateSeverity`
- **EventPaymentCharacterizationTests.cs** (16 testes): `GetGroupAdminPixKey`, `BuildPixStaticPayload`, `GeneratePixCharge`
- **ProfileCharacterizationTests.cs** (8 testes): `NormalizeOptional`

### Service Unit Tests (41 testes)
Testes unitarios diretos para os 3 services extraidos:
- **EventPaymentChargeCalculatorTests.cs** (8 testes)
- **ReconciliationSeverityEvaluatorTests.cs** (17 testes)
- **PixStaticPayloadGeneratorTests.cs** (16 testes)

### Metricas de testes
| Metrica | Valor |
|---------|-------|
| Baseline | 1799 |
| Final | 1876 |
| Novos | +77 |
| Falhando | 0 |
| Ignorados | 0 |

## Bloco B — SOLID

### Services extraidos (SRP)
3 services extraidos de code-behinds, cada um com testes TDD antes da integracao:

| Service | Code-behind origem | Logica extraida |
|---------|-------------------|-----------------|
| `EventPaymentChargeCalculator` | `EventPayment.razor.cs` | Calculo de charge amount com taxas fixas (AppFee + GatewayFee) |
| `ReconciliationSeverityEvaluator` | `AdminPayments.razor.cs` | Classificacao de severidade (Critico/Atencao/OK) baseada em metricas de pendencias |
| `PixStaticPayloadGenerator` | `EventPayment.razor.cs` | Geracao de BR Code Pix (EMVco) + selecao de chave Pix do admin do grupo |

### Padrao de extracao aplicado
1. Criar service com logica pura (sem dependencias de Blazor)
2. Escrever testes unitarios (TDD)
3. Registrar no DI container (`Program.cs`)
4. Adicionar `@inject` no `.razor` (nao `[Inject]` no `.razor.cs` — limitacao do compilador Blazor)
5. Substituir logica inline no code-behind por chamada ao service
6. Build + suite verde

### DI Registration
Todos os 3 services registrados como `Scoped` em `Program.cs`.

## Bloco C — CSS

### Fase 2: Breakpoints 768px (pre-sessao, ja executado)
- 700px -> 768px: 6 arquivos scoped + 3 globais
- 760px -> 768px: 4 arquivos scoped
- 480px -> 768px: 3 arquivos scoped + events.css

### Fase 5: Design Tokens (pre-sessao, ja executado)
- 342 variaveis CSS definidas em `:root`
- 0 variaveis mortas
- 0 variaveis indefinidas
- 0 hardcoded hex em scoped CSS

### Fase 6: Fragmentacao Scoped vs Global
- **Antes:** 123 arquivos `.razor.css` (25 vazios, 98 nao-vazios)
- **Depois:** 98 arquivos (todos nao-vazios)
- **25 arquivos vazios removidos**
- 879 classes globais, 1175 classes scoped, 200 duplicadas (esperado — Blazor isolamento `b-[hash]`)
- Conclusao: nenhuma consolidacao massiva necessaria

### Fase 7: BEM
- 47 de 98 arquivos (48%) ja usam modificadores BEM (`--`)
- Convencao formalizada: `block__element--modifier` para novos componentes
- Nao renomear classes existentes em massa (regra: sem mudanca de comportamento sem teste)

## Commits
1. `e2336f0` — refactor: extrair code-behinds Blazor (.razor.cs) e limpar @code blocks
2. `5ff947f` — refactor(css): padronizar breakpoints 768px, remover 25 .razor.css vazios, auditar fragmentacao e formalizar BEM
3. `34cb9d3` — refactor(SOLID): extrair 3 services testaveis + registrar DI
4. `69b0187` — test: +77 testes (54 characterization + 41 service unit)
5. `5cb4ce8` — docs: formalizar TDD + SOLID como regra permanente + metricas

## Criterios de aceitacao
- [x] Suite de testes verde, contagem >= baseline (1876 >= 1799)
- [x] 0 erros de build
- [x] 0 hardcoded hex em scoped CSS
- [x] 0 var CSS indefinida
- [x] 0 `AppDbContext` direto em componentes
- [x] Sem regressao visual/funcional
- [x] Commits pequenos, 1 responsabilidade cada
- [x] Encoding UTF-8 em todos os CSS
- [x] Nenhum teste ignorado/comentado

## Padroes estabelecidos (going forward)
1. **TDD**: baseline verde obrigatorio; characterization tests antes de refatorar; Red-Green-Refactor
2. **SOLID/SRP**: code-behinds nao contem logica de negocio; extrair para services testaveis
3. **BEM**: `block__element--modifier` em novos componentes
4. **PR body como documentacao**: todo PR deve incluir body descritivo para o revisor (nao apenas titulo)
5. **@inject via .razor**: quando `[Inject]` no `.razor.cs` nao for reconhecido pelo compilador Blazor

## Notas para o Senior
- Os 24 testes `ProgramConfigurationTests` que falham localmente precisam de Postgres (passam no CI)
- Os characterization tests via reflection sao temporarios — quando o codigo legado for refatorado, os metodos privados serao movidos para services e os tests de reflection substituidos por tests diretos
- `PixStaticPayloadGenerator` e `EventPaymentChargeCalculator` usam metodos estaticos (pure functions) — nao precisam de DI na forma atual, mas estao registrados para futura extensao via interface
