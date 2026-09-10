using Confirmai.Services.Core.UiText;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Anti-hardcode test: ensures every i18n key in PtBr exists in EnUs and EsEs
/// (and vice versa) across all text provider classes. Fase 6, Ciclo 26. Fase E,
/// Ciclo 27: removed the silent `if (enUs.Count == 0) return;`. C30-C Fase 2:
/// the 3 product dictionaries (FutsalTexts, GroupTexts, PokerTexts) now have
/// full EN/ES and are included — PtBrOnlyProviders is empty.
/// </summary>
public class I18nKeyParityTests
{
    /// <summary>
    /// Providers with full PT-BR/EN-US/ES-ES translations. The three V1 PT-BR-only
    /// stubs (FutsalTexts, GroupTexts, PokerTexts) are deliberately excluded —
    /// they have no EN/ES to compare against. A silent `return` that disabled the
    /// test for them was removed; the exclusion is now explicit in the data.
    /// </summary>
    public static IEnumerable<object[]> TextProviders => new[]
    {
        new object[] { "AdminTexts", AdminTexts.PtBr, AdminTexts.EnUs, AdminTexts.EsEs },
        new object[] { "AuthTexts", AuthTexts.PtBr, AuthTexts.EnUs, AuthTexts.EsEs },
        new object[] { "CoreTexts", CoreTexts.PtBr, CoreTexts.EnUs, CoreTexts.EsEs },
        new object[] { "FutsalTexts", FutsalTexts.PtBr, FutsalTexts.EnUs, FutsalTexts.EsEs },
        new object[] { "GroupTexts", GroupTexts.PtBr, GroupTexts.EnUs, GroupTexts.EsEs },
        new object[] { "PaymentTexts", PaymentTexts.PtBr, PaymentTexts.EnUs, PaymentTexts.EsEs },
        new object[] { "PokerTexts", PokerTexts.PtBr, PokerTexts.EnUs, PokerTexts.EsEs },
        new object[] { "ServerTexts", ServerTexts.PtBr, ServerTexts.EnUs, ServerTexts.EsEs },
        new object[] { "UtilityTexts", UtilityTexts.PtBr, UtilityTexts.EnUs, UtilityTexts.EsEs },
    };

    /// <summary>
    /// PT-BR-only providers (V1 decision). Empty since C30-C Fase 2: all 3
    /// product dictionaries (FutsalTexts, GroupTexts, PokerTexts) now have
    /// full EN-US/ES-ES translations and are in TextProviders above.
    /// Kept as a visible marker that the exclusion no longer exists.
    /// </summary>
    public static IReadOnlyList<string> PtBrOnlyProviders { get; } = Array.Empty<string>();

    [Theory]
    [MemberData(nameof(TextProviders))]
    public void PtBr_And_EnUs_HaveSameKeys(string name,
        IReadOnlyDictionary<string, string> ptBr,
        IReadOnlyDictionary<string, string> enUs,
        IReadOnlyDictionary<string, string> _)
    {
        var ptKeys = ptBr.Keys.ToHashSet();
        var enKeys = enUs.Keys.ToHashSet();
        var missingInEn = ptKeys.Except(enKeys).ToList();
        var extraInEn = enKeys.Except(ptKeys).ToList();

        Assert.True(missingInEn.Count == 0,
            $"{name}: keys missing in EnUs: {string.Join(", ", missingInEn)}");
        Assert.True(extraInEn.Count == 0,
            $"{name}: extra keys in EnUs (not in PtBr): {string.Join(", ", extraInEn)}");
    }

    [Theory]
    [MemberData(nameof(TextProviders))]
    public void PtBr_And_EsEs_HaveSameKeys(string name,
        IReadOnlyDictionary<string, string> ptBr,
        IReadOnlyDictionary<string, string> _,
        IReadOnlyDictionary<string, string> esEs)
    {
        var ptKeys = ptBr.Keys.ToHashSet();
        var esKeys = esEs.Keys.ToHashSet();
        var missingInEs = ptKeys.Except(esKeys).ToList();
        var extraInEs = esKeys.Except(ptKeys).ToList();

        Assert.True(missingInEs.Count == 0,
            $"{name}: keys missing in EsEs: {string.Join(", ", missingInEs)}");
        Assert.True(extraInEs.Count == 0,
            $"{name}: extra keys in EsEs (not in PtBr): {string.Join(", ", extraInEs)}");
    }

    [Theory]
    [MemberData(nameof(TextProviders))]
    public void NoEmptyValues_InAnyLanguage(string name,
        IReadOnlyDictionary<string, string> ptBr,
        IReadOnlyDictionary<string, string> enUs,
        IReadOnlyDictionary<string, string> esEs)
    {
        var emptyPt = ptBr.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
        Assert.True(emptyPt.Count == 0, $"{name}: empty values in PtBr: {string.Join(", ", emptyPt)}");

        var emptyEn = enUs.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
        Assert.True(emptyEn.Count == 0, $"{name}: empty values in EnUs: {string.Join(", ", emptyEn)}");

        var emptyEs = esEs.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
        Assert.True(emptyEs.Count == 0, $"{name}: empty values in EsEs: {string.Join(", ", emptyEs)}");
    }
}
