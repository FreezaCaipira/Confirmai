# CSS Audit — Ciclo 19 Fase 1

## Global CSS (4 files)
| File | Lines | Hex | !important |
|------|-------|-----|------------|
| site.css | 6087 | 125 | 11 |
| events.css | 4326 | 5 | 2 |
| marketplace.css | 452 | 0 | 0 |
| identity.css | 321 | 7 | 0 |

## Scoped CSS (98 non-empty files)
- Hardcoded hex/rgba: **0**
- !important: **4** (AvatarUploadSection 1, Create 2, EventPaymentProof 1)

## Breakpoint Issues

### Must fix (mobile boundary inconsistency)
- **700px** → 768px: Index.razor.css (3), CitySelector, MainLayout, UserSummaryCard
- **760px** → 768px: Profile, MembersManager, CookieConsent, GroupDetailPendingRequests
- **480px** → 768px: App.razor.css, Create.razor.css, GroupMetrics (mobile layout)

### Legitimate progressive breakpoints (keep)
- 1024px, 1100px, 1360px, 1440px — desktop/tablet enhancements
- 375px — very small mobile
- 300px, 320px, 340px, 360px — micro-breakpoints for small screens

### Review needed
- 860px, 980px — could be 768px or kept as tablet
- 640px — used in many files, could be 768px
- 150px, 200px, 250px — element-specific, likely fine

## Fase 2 Priority
1. Fix 700px → 768px in scoped CSS (6 files) — DONE
2. Fix 760px → 768px in scoped CSS (4 files) — DONE
3. Fix 480px → 768px in scoped CSS (3 files) — DONE
4. Fix 700px → 768px in global CSS (events.css, identity.css, marketplace.css) — DONE
5. Fix 760px → 768px in global CSS (site.css) — DONE (site.css had no 700/760, already 768)

## Fase 2 — Completed
- 700px → 768px: Groups/Index.razor.css, CitySelector.razor.css, UserSummaryCard.razor.css, events.css, identity.css, marketplace.css
- 760px → 768px: Profile.razor.css, GroupDetailPendingRequests.razor.css, CookieConsent.razor.css, MembersManager.razor.css
- 480px → 768px (mobile layout): GroupMetrics.razor.css, App.razor.css, Create.razor.css
- 480px progressive refinements in site.css (sidebar, header emblem) — kept as intentional
- Ciclo 18 fixes: events.css 480px→768px, Payments.razor.css 480px→768px, RankingTable.razor.css 480px→768px

## Fase 5 — Design Tokens: Completed
- Total vars defined in site.css :root: 342
- Total vars used across all CSS: 343
- Dead vars: 0 (7 false positives — comments/class names, not real var definitions)
- Undefined vars: 1 real issue (--orange-soft in AdminRevenue.razor.css) — FIXED → --amber-mid
- 3 other "undefined" are safe: --accent-preview (inline style), --item-accent-border (has fallback), --tp-ok-bg (local scoped var)
- Hardcoded hex in scoped CSS: 0
- identity.css vars (--identity-rhythm-*) defined in identity.css, not site.css — expected

## Fase 6 — Scoped vs Global Fragmentation Audit

### Summary
- **Scoped CSS files (before):** 123 (25 empty, 98 non-empty, ~15,004 lines)
- **Scoped CSS files (after cleanup):** 98 (all non-empty)
- **Global CSS files:** 4 (site.css 6087, events.css 4326, marketplace.css 452, identity.css 321)
- **Global classes:** 879 unique
- **Scoped classes:** 1175 unique
- **Classes in both global and scoped:** 200

### Action taken
- **Deleted 25 empty .razor.css files** — no content, no impact

### Analysis of 200 duplicate classes
Most duplicates are expected Blazor scoped CSS pattern (isolation via `b-[hash]` attribute):
- **Utility classes** (`active`, `main`, `input`, `label`, `error`, `pagination`) — scoped overrides are intentional
- **Component-specific classes** (`entity-shell-*`, `filter-bar`, `form-group`) — used in scoped for isolation
- **Marketplace classes** (`mk-*`) — scoped to marketplace pages only

### Recommendation (going forward)
- No mass consolidation needed — Blazor's scoped CSS isolation prevents actual style conflicts
- New components should follow BEM (TODO #5) to reduce class name collisions
- Avoid creating empty .razor.css files — only create when there are styles to add

## Fase 7 — BEM Adoption Audit

### Current state
- **Files using BEM modifiers (`--`):** 47 of 98 (48%)
- **Files not using BEM:** 51 of 98 (52%)
- BEM is already well-established in the codebase (e.g., `evpay-status--paid`, `sport-card--futsal`, `ranking-medal--gold`)

### Convention (going forward)
All **new** components and CSS files must follow BEM:
- **Block:** standalone entity (e.g., `.event-card`)
- **Element:** child of block (e.g., `.event-card__title`)
- **Modifier:** state/variant (e.g., `.event-card--confirmed`)

Rules:
1. Use `block--modifier` for state variants (already common)
2. Use `block__element` for nested elements (underused — adopt in new components)
3. No mass renaming of existing classes (refactor rule: no behavioral changes without tests)
4. When refactoring a component's CSS, convert to BEM at that time
