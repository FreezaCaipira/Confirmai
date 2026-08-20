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
        // Allowlist is now empty — all residuals have been migrated to i18n.
        // Date format patterns with "às" were moved to UiTextService.FormatDateTime
        // (DateTimeFull / DateTimeFullLong keys) in Ciclo 29 Fase C.
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
}
