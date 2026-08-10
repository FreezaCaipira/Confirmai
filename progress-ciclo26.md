# Ciclo 26 — Progresso e Próximos Passos

## Branch
`refactor/ciclo26-profile-ux-fee-ledger` (pushed to origin)

## Objetivo
Refatorar UX do Profile + implementar Fee Ledger para pagamentos manuais de futsal (taxa R$ 0,75).

## Fases Concluídas (7/7)

### Fase 0 — Privacidade Pix ✅
- Pix key só visível para o dono do perfil
- `ProfilePixVisibility` helper + 4 testes

### Fase 1 — UX do Profile ✅
- Header + avatar unificados, EditForm sempre visível
- Bloco Pix com `#pix` + deep link `?intent=pix`
- 4 chaves i18n novas (PT/EN/ES)

### Fase 2 — Ledger da Taxa R$ 0,75 ✅
- `PlatformFeeAmount` (decimal?) em `EventConfirmation`
- `ManualPlatformFeeFixed = 0.75` em `appsettings.json` (`FeeOptions`)
- `PlatformFeeLedgerService`: stamp fee on paid + group balance
- Migration `AddPlatformFeeLedger`
- 10 testes

### Fase 3 — Repasse com Comprovante ✅
- `PlatformFeeSettlement` model (GroupId, Amount, ProofImage, Status, Review)
- `PlatformFeeSettlementService`: submit + admin review (approve/reject)
- 8 testes

### Fase 4 — Cobrança ao Jogador ✅
- `EventPaymentSummary` mostra breakdown: "Partida + Taxa = Total"
- `ShowFeeBreakdown` param (V1 manual: futsal, sem gateway, preço > 0)
- 2 chaves i18n novas (SummaryMatchPrice, SummaryPlatformFee)
- 5 testes

### Fase 5 — Pix como Pré-requisito ✅
- `FutsalCreateService.SaveAsync` bloqueia criação de partida com preço se admin não tem Pix
- Erro retorna link `/profile/{userId}?intent=pix`
- 4 testes

### Fase 6 — i18n Anti-Hardcode ✅
- `I18nKeyParityTests`: 27 testes (paridade PT/EN/ES + valores vazios)
- Skip stubs intencionais (FutsalTexts, GroupTexts, PokerTexts — EnUs/EsEs vazios)
- Fix `FutsalCreateServiceTests`: seeded users agora têm PixKey

### Fix Extra — EF Core Snapshot ✅
- `DefaultEnablePaymentGatewaysFalse.Designer.cs` foi criado como `ModelSnapshot` em vez de migration designer
- Corrigido para `[Migration]` + `BuildTargetModel`
- `AppDbContextModelSnapshot.cs` atualizado com PlatformFeeSettlement
- Removido snapshot duplicado

## Métricas
- 0 warnings no build
- 2223 testes verdes (49 novos no ciclo)
- 24 failures pré-existentes (ProgramConfigurationTests — precisam web server)

## Commits (8)
1. `c5d81ae` — Fase 0: privacidade Pix
2. `19365dd` — Fase 1: UX do Profile
3. `96c870c` — Fase 2: ledger da taxa
4. `e3d9c4f` — Fase 3: repasse com comprovante
5. `83be3fc` — Fase 4: cobrança ao jogador
6. `018bfe9` — Fase 5: Pix como pré-requisito
7. `84cccc4` — Fase 6: i18n anti-hardcode
8. `fdbdd87` — Fix: snapshot EF Core

## Próximos Passos
1. **Abrir PR** da branch `refactor/ciclo26-profile-ux-fee-ledger` → `main`
2. **Review do PR** no PC principal
3. **Merge** após aprovação
4. **Ciclo 27** — possíveis próximos trabalhos:
   - Completar EN/ES dos stubs (FutsalTexts, GroupTexts, PokerTexts)
   - UI do admin para visualizar ledger e settlements pendentes
   - Integração do `PlatformFeeSettlementService` com páginas Blazor (upload de comprovante)
   - Testes E2E do fluxo completo (criar partida → pagar → repasse → confirmação)

## Como continuar no PC principal
```bash
git fetch origin
git checkout refactor/ciclo26-profile-ux-fee-ledger
dotnet build
dotnet test
```
