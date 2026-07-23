# Ciclo 18: TDD + SOLID + CSS Refactoring — Dashboard de Progresso

> Atualizado em 22/07/2026 | Base: `main` (pós-Ciclo 17)
> 1.781/1.781 testes passando | 0 erros de build

---

## Visão Geral dos 3 Objetivos

| Objetivo | Status | Progresso |
|----------|--------|-----------|
| **A. CSS Refactoring** (Boas práticas + SOLID CSS) | 🟡 Em andamento | ~70% |
| **B. SOLID do projeto** (code-behind extraction, SRP) | 🟡 Em andamento | ~60% |
| **C. TDD retroativo** (cobertura de código legado) | 🔴 Início | ~20% |

---

## A. CSS Refactoring — Detalhamento

### Fases do Bloco C (conforme WORK_PLAN.md)

| Fase | Descrição | Status | Detalhe |
|------|-----------|--------|---------|
| C1 | Inventário/auditoria CSS | ✅ Done | `css-audit.md` completo |
| C2 | Design Tokens (vars, zero hardcoded scoped) | ✅ Done | 0 hex em scoped, 342 vars no `:root`, 0 vars mortas |
| C3 | Mobile-first + breakpoint único 768px | ✅ Done | 700px→768px, 760px→768px, 480px→768px todos corrigidos |
| C4 | Single Responsibility por `.razor.css` | 🟡 Parcial | `oldsite-top-nav` consolidado (Ciclo 17). Outros casos pendentes |
| C5 | BEM em scoped novo/refatorado | 🔴 Pendente | Não iniciado |
| C6 | Opcionais (`@layer`, TDD visual Playwright) | 🔴 Pendente | Não iniciado (experimental) |

### Métricas CSS Atuais

| Métrica | Valor | Meta |
|---------|-------|------|
| Hardcoded hex em scoped CSS | **0** | 0 ✅ |
| `!important` em scoped CSS | **4** | Reduzir |
| `!important` em global CSS | **13** | 11 legítimos + 2 novos? Investigar |
| Vars no `:root` | **342** | Manter |
| Vars mortas | **0** | 0 ✅ |
| Vars indefinidas | **0** | 0 ✅ |
| Breakpoints inconsistentes | **0** (768px padronizado) | 0 ✅ |

### Próximos passos CSS
1. **C4**: Auditar fragmentação CSS restante (scoped vs global conflitos)
2. **C5**: Adotar BEM em novos componentes/refatorados
3. Investigar 2 `!important` globais adicionais (13 vs 11 do C15)

---

## B. SOLID do Projeto — Detalhamento

### O que foi feito (code-behind extraction = SRP)

| Métrica | Antes | Agora | Status |
|---------|-------|-------|--------|
| Arquivos `.razor` com `@code` inline | ~80 | **1** (Users.razor, comentado) | ✅ |
| Code-behind `.razor.cs` files | ~19 (pré-existentes) | **99** | ✅ |
| `@inject` → `[Inject]` conversions | 0 | **~80** | ✅ |
| `AppDbContext` direto em componentes | 0 | **0** | ✅ mantido |

### O que falta (SOLID mais amplo)

| Item | Status | Prioridade |
|------|--------|------------|
| **SRP**: Extrair lógica de negócio de code-behinds para services testáveis | 🔴 Pendente | Alta |
| **OCP/DIP**: Pontos de extensão via interface + DI | 🟡 Parcial | Média |
| **LSP/ISP**: Interfaces pequenas e coesas | 🟡 Parcial | Média |
| **Blazor**: Minimizar lógica em markup, componentizar blocos repetidos | 🟡 Parcial | Alta |
| Auditoria de services/classes que violam SRP | 🔴 Pendente | Alta |

### Próximos passos SOLID
1. **Auditar code-behinds grandes**: identificar `.razor.cs` com >100 linhas de lógica de negócio (não UI) e extrair para services
2. **Mapear dependências**: identificar classes concretas injetadas vs interfaces
3. **Componentizar**: identificar markup repetido entre páginas e extrair componentes

---

## C. TDD Retroativo — Detalhamento

### Regra de ouro (do Senior)
> "Antes de refatorar qualquer coisa, tem que existir teste verde cobrindo o comportamento atual. Se não existe, escreve o teste primeiro (characterization), ve passar, e só então refatora."

### Métricas TDD

| Métrica | Valor | Meta |
|---------|-------|------|
| Total de testes | **1.781** | Crescente |
| Arquivos de teste | **212** | Crescente |
| Services sem teste | **?** (auditar) | 0 |
| Code-behinds sem teste | **~99** (quase todos) | Cobrir lógica crítica |
| Characterization tests escritos | **0** | Antes de refatorar |
| TDD como regra going forward | 🔴 Não formalizado | Adotar |

### Cobertura atual por área (estimativa)

| Área | Testes? | Cobertura |
|------|---------|-----------|
| Services (Payment, Group, Admin, etc.) | ✅ Sim | Boa — services têm testes unitários |
| Integration tests (Futsal, Groups, Payment) | ✅ Sim | Razoável — fluxos principais |
| Code-behind logic (`.razor.cs`) | ❌ Não | Baixa — quase zero |
| CSS/Visual regression | ❌ Não | Zero |
| Configuration/Program | ✅ Sim | Excluídos do filtro (precisam Postgres) |

### Próximos passos TDD
1. **Auditar services sem teste**: cruzar `Services/*.cs` vs `Tests/*.cs`
2. **Characterization tests para code-behinds críticos**: antes de extrair lógica SOLID, escrever testes que capturam comportamento atual
3. **Estabelecer regra TDD**: todo novo comportamento → teste que falha → código mínimo → refatorar
4. **Testar code-behinds com lógica de negócio**: identificar `.razor.cs` com lógica testável e escrever testes

---

## Linha do Tempo dos Ciclos

| Ciclo | Foco | Status |
|-------|------|--------|
| 1-7 | CSS cleanup, testes, UX | ✅ Concluído |
| 8-11 | rgba cleanup, decomposição de páginas, code-behind inicial | ✅ Concluído |
| 12-13 | UX fixes, background, IDbContextFactory zerado | ✅ Concluído |
| 14 | Refresh token / sessão persistente | ✅ Concluído |
| 15 | Testes + UX + !important cleanup | ✅ Concluído |
| 16 | Mobile UX (menu hamburger) | ✅ Concluído |
| 17 | Login Google + email real + mojibake + menu mobile | ✅ Concluído |
| Pagamento Real | Taxa fixa + repasse automático | ✅ Implementado (revisado) |
| **18** | **TDD + SOLID + CSS Refactoring** | **🟡 Em andamento** |

---

## Próximas Ações Recomendadas (prioridade)

1. **TDD**: Auditar services sem teste → escrever characterization tests para code-behinds críticos
2. **SOLID**: Auditar `.razor.cs` com >100 linhas → extrair lógica de negócio para services testáveis
3. **CSS C4**: Auditar fragmentação scoped vs global restante
4. **CSS C5**: Adotar BEM em novos componentes
5. **Formalizar regras**: TDD + SOLID como regra going forward no WORK_PLAN.md
