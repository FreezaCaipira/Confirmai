using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Anti-hardcode i18n convention test (Fase A, Ciclo 28 — beyond accented chars).
/// Scans every .razor file under Pages/ and Shared/ for raw visible Portuguese
/// literals in element text, title, placeholder, alt, aria-label, and PageTitle.
/// Detects both accented characters AND common unaccented Portuguese words.
/// Fails when a literal is not in the allowlist.
///
/// The allowlist is explicit and short: brand names, proper nouns, symbols,
/// and a documented set of residuals being migrated incrementally. Each
/// allowlisted entry has a comment explaining why it is there. This is NOT
/// a silent `return` that disables the test — it is a visible, commented
/// exception list that shrinks over time.
/// </summary>
public class AntiHardcodeI18nTests
{
    // Accented Portuguese characters that signal a raw visible literal.
    private static readonly Regex AccentedCharRegex = new(
        @"[áéíóúÁÉÍÓÚãõÃÕçÇâêôÂÊÔàèìòùÀÈÌÒÜüïñÑ]",
        RegexOptions.Compiled);

    // Common unaccented Portuguese words that signal a raw visible literal.
    // These are words so common in PT UI that their presence in a .razor file
    // almost certainly means a hardcoded string instead of an i18n key.
    // Short words (<= 3 chars) are excluded to avoid false positives (articles,
    // prepositions that appear in CSS class names, expressions, etc.).
    private static readonly HashSet<string> PtUnaccentedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Voltar", "Grupos", "Total", "Enviar", "Salvar", "Cancelar", "Nome",
        "Data", "Status", "Valor", "Preco", "Usuario", "Senha", "Email",
        "Buscar", "Filtrar", "Limpar", "Editar", "Criar", "Excluir", "Remover",
        "Adicionar", "Confirmar", "Rejeitar", "Aprovar", "Pendente", "Pago",
        "Jogador", "Partida", "Grupo", "Evento", "Perfil", "Conta", "Pagamento",
        "Comprovante", "Repasse", "Taxa", "Historico", "Resumo", "Detalhes",
        "Configuracoes", "Notificacoes", "Mensagem", "Erro", "Sucesso",
        "Carregando", "Nenhum", "Nenhuma", "Disponivel", "Indisponivel",
        "Ativo", "Inativo", "Sim", "Nao", "Todos", "Todas", "Novo", "Nova",
        "Anterior", "Proximo", "Primeiro", "Ultimo", "Inicio", "Fim",
        "Vitorias", "Derrotas", "Empates", "Jogos", "Pontos", "Posicao",
        "Ranking", "Classificacao", "Estatisticas", "Presenca", "Faltas",
        "Organizador", "Admin", "Administrador", "Membro", "Membros",
        "Solicitacao", "Solicitacoes", "Aprovar", "Rejeitar", "Pendente",
        "Pendentes", "Atrasado", "Atrasados", "Vencido", "Vencidos",
        "Receber", "Pagar", "Pago", "Divida", "Saldo", "Credito",
        "Chave", "Codigo", "Telefone", "Endereco", "Cidade", "Estado",
        "Bairro", "Rua", "Numero", "Complemento", "Cep",
        "Segunda", "Terca", "Quarta", "Quinta", "Sexta", "Sabado", "Domingo",
        "Janeiro", "Fevereiro", "Marco", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro",
        "Hoje", "Ontem", "Amanha", "Semana", "Mes", "Ano",
        "Min", "Max", "Maximo", "Minimo", "Quantidade", "Qtd",
        "Tipo", "Categoria", "Descricao", "Observacao", "Nota",
        "Acao", "Acoes", "Visualizar", "Baixar", "Imprimir", "Compartilhar",
        "Login", "Logout", "Registrar", "Cadastrar", "Esqueci", "Lembrar",
        "Termos", "Privacidade", "Sobre", "Ajuda", "Suporte", "Contato",
        "Configurar", "Habilitar", "Desabilitar", "Ativar", "Desativar",
        "Pausar", "Continuar", "Iniciar", "Parar", "Resetar", "Atualizar",
        "Recarregar", "Sincronizar", "Exportar", "Importar",
        "Selecionar", "Selecionado", "Selecionados", "Marca", "Marcado",
        "Desmarcar", "Expandir", "Recolher", "Mostrar", "Ocultar",
        "Visivel", "Oculto", "Publico", "Privado",
        "Casa", "Fora", "Mandante", "Visitante",
        "Gol", "Gols", "Cartao", "Amarelo", "Vermelho",
        "Substituicao", "Substituicoes", "Escalacao", "Titular", "Reserva",
        "Reservas", "Bancao", "Waitlist", "Lista",
    };

    // Visible attributes that should not contain raw Portuguese literals.
    private static readonly Regex VisibleAttributeRegex = new(
        @"(?:placeholder|title|alt|aria-label)\s*=\s*""([^""]*)""",
        RegexOptions.Compiled);

    // <PageTitle> content.
    private static readonly Regex PageTitleRegex = new(
        @"<PageTitle>\s*(.*?)\s*</PageTitle>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Element text content (between > and <) — captures text with accents OR
    // common unaccented PT words.
    private static readonly Regex ElementTextRegex = new(
        @">\s*([^<>{]+)\s*<",
        RegexOptions.Compiled);

    // Lines to skip: Blazor directives, comments, code blocks, CSS, @attributes.
    // Only skip pure code/directive lines — if a line contains HTML tags (<),
    // it may have visible text and should be scanned.
    private static readonly Regex SkipLineRegex = new(
        @"^\s*(?:@code|@inject|@using|@page|@attribute|@implements|@inherits|@layout|@namespace|@typeparam|@preservewhitespace|//|/\*|\*)",
        RegexOptions.Compiled);

    // ── Allowlist ──────────────────────────────────────────────────────────
    // Brand names and proper nouns that are intentionally not translated.
    private static readonly HashSet<string> BrandAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmai", "Confirma Aí!", "Bora jogar!?"
    };

    // Residuals accepted for now (to be migrated in a future cycle).
    // Each entry is "fileName|literal" — documented, not silent.
    // This list should shrink over time as literals are migrated to i18n keys.
    private static readonly HashSet<string> ResidualAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        // Empty — .razor residuals migrated in C29 Fase C and the .razor.cs
        // code-behind debt (39 literals) migrated in C33 Fase 4. Any NEW
        // violation fails the test; do not re-add entries here, migrate the
        // literal to an i18n key instead.
    };

    private static IEnumerable<string> GetRazorFiles()
    {
        var baseDir = AppContext.BaseDirectory;
        // Walk up from bin/Debug/net9.0 to find the main project root
        // (a directory that has both a .csproj file and a Pages/ folder).
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                break;
            dir = dir.Parent;
        }
        if (dir is null) yield break;

        foreach (var sub in new[] { "Pages", "Shared" })
        {
            var subDir = Path.Combine(dir.FullName, sub);
            if (!Directory.Exists(subDir)) continue;
            foreach (var f in Directory.GetFiles(subDir, "*.razor", SearchOption.AllDirectories))
                yield return f;
        }
    }

    private static IEnumerable<string> GetRazorCodeBehindFiles()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                break;
            dir = dir.Parent;
        }
        if (dir is null) yield break;

        foreach (var sub in new[] { "Pages", "Shared" })
        {
            var subDir = Path.Combine(dir.FullName, sub);
            if (!Directory.Exists(subDir)) continue;
            foreach (var f in Directory.GetFiles(subDir, "*.razor.cs", SearchOption.AllDirectories))
                yield return f;
        }
    }

    [Fact]
    public void RazorFiles_NoRawPortugueseLiterals_OutsideAllowlist()
    {
        var violations = new List<string>();
        var filesScanned = 0;
        var linesScanned = 0;

        foreach (var file in GetRazorFiles())
        {
            filesScanned++;
            var fileName = Path.GetFileNameWithoutExtension(file);
            var relativePath = file.Replace('\\', '/');
            var lines = File.ReadAllLines(file);

            // Skip commented-out files (entire file wrapped in @* *@)
            var nonCommentContent = StripComments(string.Join("\n", lines));
            if (string.IsNullOrWhiteSpace(nonCommentContent)) continue;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;
                linesScanned++;

                // Skip Blazor directives, code, comments.
                if (SkipLineRegex.IsMatch(line)) continue;

                // Check visible attributes (placeholder, title, alt, aria-label).
                var attrMatches = VisibleAttributeRegex.Matches(line);
                foreach (Match m in attrMatches)
                {
                    var value = m.Groups[1].Value;
                    if (IsPortugueseLiteral(value) && !IsAllowed(fileName, value))
                        violations.Add($"{relativePath}:{lineNum} attr=\"{value}\"");
                }

                // Check element text content.
                var textMatches = ElementTextRegex.Matches(line);
                foreach (Match m in textMatches)
                {
                    var text = m.Groups[1].Value.Trim();
                    if (text.Length > 0 && IsPortugueseLiteral(text) && !IsAllowed(fileName, text))
                        violations.Add($"{relativePath}:{lineNum} text=\"{text}\"");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Scanned {filesScanned} files, {linesScanned} lines. " +
            $"Raw Portuguese literals found in .razor files (not in allowlist):\n" +
            string.Join("\n", violations.Take(50)) +
            (violations.Count > 50 ? $"\n... and {violations.Count - 50} more." : ""));
    }

    /// <summary>
    /// Scans .razor.cs code-behind files for hardcoded Portuguese string literals.
    /// Catches strings that the .razor scan misses (feedback messages, error
    /// messages, validation messages assigned in C# code).
    /// Skips i18n key lookups (Ui["..."], T["..."], _t["..."]) — the key is an
    /// identifier, not visible text. Also skips using/namespace/file-path strings.
    /// </summary>
    [Fact]
    public void RazorCodeBehind_NoRawPortugueseLiterals_OutsideAllowlist()
    {
        var violations = new List<string>();
        var filesScanned = 0;

        // Regex to find string literals in double quotes.
        var stringLiteralRegex = new Regex(@"""([^""]*)""", RegexOptions.Compiled);

        // Lines to skip: using, namespace, [Attribute(...)], //
        var skipCodeLineRegex = new Regex(
            @"^\s*(?:using|namespace|\[|//|/\*|\*|///)",
            RegexOptions.Compiled);

        // Patterns that are NOT visible text: i18n key lookups, file paths,
        // format strings with only placeholders, CSS class names, HTML tags.
        var i18nKeyLookupRegex = new Regex(
            @"(?:Ui|T|_t|_T)\s*\[""",
            RegexOptions.Compiled);

        // Lines that are audit/log calls — these are internal, not user-facing.
        var auditLogLineRegex = new Regex(
            @"(?:AuditAsync|LogAsync|LogInformation|LogWarning|LogError|Console\.)\s*\(",
            RegexOptions.Compiled);

        foreach (var file in GetRazorCodeBehindFiles())
        {
            filesScanned++;
            var relativePath = file.Replace('\\', '/');
            var lines = File.ReadAllLines(file);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;

                if (skipCodeLineRegex.IsMatch(line)) continue;
                if (auditLogLineRegex.IsMatch(line)) continue;

                // Find all string literals on this line.
                var matches = stringLiteralRegex.Matches(line);
                foreach (Match m in matches)
                {
                    var value = m.Groups[1].Value;

                    // Skip empty or very short strings.
                    if (string.IsNullOrWhiteSpace(value) || value.Length < 4) continue;

                    // Skip i18n key lookups — the key is an identifier, not text.
                    var beforeQuote = line[..m.Index];
                    if (i18nKeyLookupRegex.IsMatch(beforeQuote)) continue;

                    // Skip strings that look like file paths, URLs, or code identifiers.
                    if (value.Contains('/') || value.Contains('\\') || value.Contains("http")) continue;
                    if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                    // Skip CSS class names (contain hyphens, no spaces).
                    if (value.Contains('-') && !value.Contains(' ')) continue;

                    // Skip strings that are clearly format strings with no visible text.
                    if (Regex.IsMatch(value, @"^[{0-9}\s,.\-:/%]+$")) continue;

                    // Skip strings with code interpolation (contain { and method calls).
                    if (value.Contains("{") && (value.Contains("Math.") || value.Contains("."))) continue;

                    // Skip single-word identifiers (no spaces — likely role names, enums).
                    if (!value.Contains(' ')) continue;

                    // Check for Portuguese literals.
                    // For .razor.cs files, GetFileNameWithoutExtension returns
                    // "Detail.razor" (strips only .cs). Strip .razor too so the
                    // allowlist key matches the .razor convention (just "Detail").
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    baseName = Path.GetFileNameWithoutExtension(baseName);
                    if (IsPortugueseLiteral(value) && !IsAllowed(baseName, value))
                        violations.Add($"{relativePath}:{lineNum} \"{value}\"");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Scanned {filesScanned} .razor.cs files. " +
            $"Raw Portuguese literals found in code-behind (not in allowlist):\n" +
            string.Join("\n", violations.Take(50)) +
            (violations.Count > 50 ? $"\n... and {violations.Count - 50} more." : ""));
    }

    /// <summary>
    /// Returns true if the text contains accented PT characters OR at least
    /// one common unaccented PT word (as a whole word, case-insensitive).
    /// Skips text that is primarily a Blazor expression (starts with @ or
    /// contains @() blocks) to avoid flagging code as a literal.
    /// </summary>
    private static bool IsPortugueseLiteral(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Skip Blazor expressions: @variable, @(expression), @T["..."], @Ui["..."]
        // These are code, not visible literals. But if there's visible PT text
        // mixed in (e.g. "Voltar" outside an @()), we still want to catch it.
        // Strategy: strip @(...) blocks, @T/Ui["..."] lookups, and @Variable tokens,
        // then check the remainder for PT words.
        var stripped = Regex.Replace(text, @"@\([^)]*\)", " ");
        stripped = Regex.Replace(stripped, @"@[A-Za-z]+\s*\[""[^""]*""\]", " ");
        stripped = Regex.Replace(stripped, @"@[A-Za-z_][A-Za-z0-9_.?]*\([^)]*\)", " ");
        stripped = Regex.Replace(stripped, @"@[A-Za-z_][A-Za-z0-9_.?]*", " ");
        stripped = stripped.Trim();

        if (string.IsNullOrWhiteSpace(stripped)) return false;

        // Fast path: accented characters always flag.
        if (AccentedCharRegex.IsMatch(stripped)) return true;

        // Check for common unaccented PT words (whole-word match).
        var words = stripped.Split(new[] { ' ', '\t', ',', '.', ':', ';', '!', '?', '-', '/', '(', ')', '"', '\'' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var clean = word.Trim(new[] { '<', '>', '{', '}', '&', ';', '=' });
            if (clean.Length < 3) continue;
            if (PtUnaccentedWords.Contains(clean))
                return true;
        }

        return false;
    }

    private static bool IsAllowed(string fileName, string literal)
    {
        // Brand names are always allowed.
        foreach (var brand in BrandAllowlist)
            if (literal.Contains(brand, StringComparison.OrdinalIgnoreCase))
                return true;

        // Per-file-and-literal residual allowlist.
        var key = $"{fileName}|{literal}";
        if (ResidualAllowlist.Contains(key, StringComparer.OrdinalIgnoreCase))
            return true;

        // Also check if the literal contains any allowlisted substring
        // (for cases where the literal has extra whitespace or formatting).
        foreach (var entry in ResidualAllowlist)
        {
            var parts = entry.Split('|', 2);
            if (parts.Length == 2 && parts[0].Equals(fileName, StringComparison.OrdinalIgnoreCase)
                && literal.Contains(parts[1], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string StripComments(string content)
    {
        return Regex.Replace(content, @"@\*.*?\*@", "", RegexOptions.Singleline);
    }

    /// <summary>
    /// Missing-key guard (C33 Fase 4): every i18n key referenced via
    /// T["Key"]/Ui["Key"]/T["Key"] lookups in Pages/, Shared/ and Areas/
    /// must exist in the merged provider dictionaries. Without this, a
    /// referenced-but-undefined key silently renders the raw key name to
    /// the user (UiTextService falls back to the key itself).
    /// </summary>
    [Fact]
    public void ReferencedI18nKeys_ExistInProviders()
    {
        var definedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in new[]
        {
            Services.Core.UiText.AdminTexts.PtBr,
            Services.Core.UiText.AuthTexts.PtBr,
            Services.Core.UiText.CoreTexts.PtBr,
            Services.Core.UiText.FutsalTexts.PtBr,
            Services.Core.UiText.GroupTexts.PtBr,
            Services.Core.UiText.PaymentTexts.PtBr,
            Services.Core.UiText.PokerTexts.PtBr,
            Services.Core.UiText.ServerTexts.PtBr,
            Services.Core.UiText.UtilityTexts.PtBr,
        })
        {
            foreach (var key in provider.Keys)
                definedKeys.Add(key);
        }

        var keyLookupRegex = new Regex(
            @"(?:Ui|T|_t|_T)\s*\[\s*""(?<key>[^""]+)""\s*\]",
            RegexOptions.Compiled);
        var skipLineRegex = new Regex(
            @"^\s*(?://|/\*|\*|///|using|namespace)",
            RegexOptions.Compiled);

        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                break;
            dir = dir.Parent;
        }
        Assert.NotNull(dir);

        var violations = new List<string>();
        var filesScanned = 0;

        foreach (var sub in new[] { "Pages", "Shared", "Areas" })
        {
            var subDir = Path.Combine(dir!.FullName, sub);
            if (!Directory.Exists(subDir)) continue;
            var files = Directory.GetFiles(subDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".razor") || f.EndsWith(".razor.cs")
                    || f.EndsWith(".cshtml") || f.EndsWith(".cshtml.cs"));
            foreach (var file in files)
            {
                filesScanned++;
                var relativePath = file.Replace('\\', '/');
                var content = StripComments(File.ReadAllText(file));
                var lineNum = 0;
                foreach (var line in content.Split('\n'))
                {
                    lineNum++;
                    if (skipLineRegex.IsMatch(line)) continue;
                    foreach (Match m in keyLookupRegex.Matches(line))
                    {
                        var key = m.Groups["key"].Value;
                        if (!definedKeys.Contains(key))
                            violations.Add($"{relativePath}:{lineNum} missing key \"{key}\"");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Scanned {filesScanned} files. Referenced i18n keys not defined in any provider:\n" +
            string.Join("\n", violations.Take(50)) +
            (violations.Count > 50 ? $"\n... and {violations.Count - 50} more." : ""));
    }
}
