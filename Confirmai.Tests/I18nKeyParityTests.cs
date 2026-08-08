using Confirmai.Services.Core.UiText;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Anti-hardcode test: ensures every i18n key in PtBr exists in EnUs and EsEs
/// (and vice versa) across all text provider classes.
/// Fase 6, Ciclo 26.
/// </summary>
public class I18nKeyParityTests
{
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

    [Theory]
    [MemberData(nameof(TextProviders))]
    public void PtBr_And_EnUs_HaveSameKeys(string name,
        IReadOnlyDictionary<string, string> ptBr,
        IReadOnlyDictionary<string, string> enUs,
        IReadOnlyDictionary<string, string> _)
    {
        // Skip stubs (PT-BR baseline only, EN/ES to be completed later)
        if (enUs.Count == 0) return;

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
        // Skip stubs (PT-BR baseline only, EN/ES to be completed later)
        if (esEs.Count == 0) return;

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

        if (enUs.Count > 0)
        {
            var emptyEn = enUs.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
            Assert.True(emptyEn.Count == 0, $"{name}: empty values in EnUs: {string.Join(", ", emptyEn)}");
        }

        if (esEs.Count > 0)
        {
            var emptyEs = esEs.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
            Assert.True(emptyEs.Count == 0, $"{name}: empty values in EsEs: {string.Join(", ", emptyEs)}");
        }
    }
}
