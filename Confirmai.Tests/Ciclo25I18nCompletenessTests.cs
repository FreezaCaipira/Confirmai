using Confirmai.Services.Core.UiText;

namespace Confirmai.Tests;

/// <summary>
/// Cross-language completeness tests for Ciclo 25 i18n migration.
/// Validates key parity, non-empty values, and Phase 5 specific keys
/// across PtBr, EnUs, and EsEs for all text domain files.
/// </summary>
public class Ciclo25I18nCompletenessTests
{
    // ──────────────────────────────────────────────────────────
    //  Cross-language key parity
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void CoreTexts_AllLanguages_HaveSameKeys()
    {
        var ptBrKeys = CoreTexts.PtBr.Keys.ToHashSet();
        var enUsKeys = CoreTexts.EnUs.Keys.ToHashSet();
        var esEsKeys = CoreTexts.EsEs.Keys.ToHashSet();

        var missingInEn = ptBrKeys.Except(enUsKeys).ToList();
        var missingInEs = ptBrKeys.Except(esEsKeys).ToList();
        var extraInEn = enUsKeys.Except(ptBrKeys).ToList();
        var extraInEs = esEsKeys.Except(ptBrKeys).ToList();

        Assert.True(missingInEn.Count == 0,
            $"CoreTexts: keys missing in EnUs: {string.Join(", ", missingInEn)}");
        Assert.True(missingInEs.Count == 0,
            $"CoreTexts: keys missing in EsEs: {string.Join(", ", missingInEs)}");
        Assert.True(extraInEn.Count == 0,
            $"CoreTexts: extra keys in EnUs not in PtBr: {string.Join(", ", extraInEn)}");
        Assert.True(extraInEs.Count == 0,
            $"CoreTexts: extra keys in EsEs not in PtBr: {string.Join(", ", extraInEs)}");
    }

    [Fact]
    public void AdminTexts_AllLanguages_HaveSameKeys()
    {
        var ptBrKeys = AdminTexts.PtBr.Keys.ToHashSet();
        var enUsKeys = AdminTexts.EnUs.Keys.ToHashSet();
        var esEsKeys = AdminTexts.EsEs.Keys.ToHashSet();

        var missingInEn = ptBrKeys.Except(enUsKeys).ToList();
        var missingInEs = ptBrKeys.Except(esEsKeys).ToList();

        Assert.True(missingInEn.Count == 0,
            $"AdminTexts: keys missing in EnUs: {string.Join(", ", missingInEn)}");
        Assert.True(missingInEs.Count == 0,
            $"AdminTexts: keys missing in EsEs: {string.Join(", ", missingInEs)}");
    }

    [Fact]
    public void UtilityTexts_AllLanguages_HaveSameKeys()
    {
        var ptBrKeys = UtilityTexts.PtBr.Keys.ToHashSet();
        var enUsKeys = UtilityTexts.EnUs.Keys.ToHashSet();
        var esEsKeys = UtilityTexts.EsEs.Keys.ToHashSet();

        var missingInEn = ptBrKeys.Except(enUsKeys).ToList();
        var missingInEs = ptBrKeys.Except(esEsKeys).ToList();
        var extraInEn = enUsKeys.Except(ptBrKeys).ToList();
        var extraInEs = esEsKeys.Except(ptBrKeys).ToList();

        Assert.True(missingInEn.Count == 0,
            $"UtilityTexts: keys missing in EnUs: {string.Join(", ", missingInEn)}");
        Assert.True(missingInEs.Count == 0,
            $"UtilityTexts: keys missing in EsEs: {string.Join(", ", missingInEs)}");
        Assert.True(extraInEn.Count == 0,
            $"UtilityTexts: extra keys in EnUs not in PtBr: {string.Join(", ", extraInEn)}");
        Assert.True(extraInEs.Count == 0,
            $"UtilityTexts: extra keys in EsEs not in PtBr: {string.Join(", ", extraInEs)}");
    }

    [Fact]
    public void PaymentTexts_AllLanguages_HaveSameKeys()
    {
        var ptBrKeys = PaymentTexts.PtBr.Keys.ToHashSet();
        var enUsKeys = PaymentTexts.EnUs.Keys.ToHashSet();
        var esEsKeys = PaymentTexts.EsEs.Keys.ToHashSet();

        var missingInEn = ptBrKeys.Except(enUsKeys).ToList();
        var missingInEs = ptBrKeys.Except(esEsKeys).ToList();

        Assert.True(missingInEn.Count == 0,
            $"PaymentTexts: keys missing in EnUs: {string.Join(", ", missingInEn)}");
        Assert.True(missingInEs.Count == 0,
            $"PaymentTexts: keys missing in EsEs: {string.Join(", ", missingInEs)}");
    }

    // ──────────────────────────────────────────────────────────
    //  Non-empty values across all languages
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void CoreTexts_AllLanguages_ValuesAreNotEmpty(string lang)
    {
        var dictionary = lang switch
        {
            "EnUs" => CoreTexts.EnUs,
            "EsEs" => CoreTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"CoreTexts.{lang}: key '{kvp.Key}' has empty value");
        }
    }

    [Theory]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void AdminTexts_AllLanguages_ValuesAreNotEmpty(string lang)
    {
        var dictionary = lang switch
        {
            "EnUs" => AdminTexts.EnUs,
            "EsEs" => AdminTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"AdminTexts.{lang}: key '{kvp.Key}' has empty value");
        }
    }

    [Theory]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_AllLanguages_ValuesAreNotEmpty(string lang)
    {
        var dictionary = lang switch
        {
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"UtilityTexts.{lang}: key '{kvp.Key}' has empty value");
        }
    }

    [Theory]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void PaymentTexts_AllLanguages_ValuesAreNotEmpty(string lang)
    {
        var dictionary = lang switch
        {
            "EnUs" => PaymentTexts.EnUs,
            "EsEs" => PaymentTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"PaymentTexts.{lang}: key '{kvp.Key}' has empty value");
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Phase 5 specific keys exist in all three languages
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void CoreTexts_Phase5_IndexKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => CoreTexts.PtBr,
            "EnUs" => CoreTexts.EnUs,
            "EsEs" => CoreTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "Index.SportsAvailable",
            "Index.VolleyballCardDesc",
            "Index.BeachTennisCardDesc",
            "Index.FootvolleyCardDesc",
            "Index.ChessCardDesc",
            "Index.SportVolleyball",
            "Index.SportBeachTennis",
            "Index.SportFootvolley",
            "Index.SportChess",
            "Index.FilterAllSports",
            "Index.FilterAllPeriods",
            "Index.FilterLast7Days",
            "Index.FilterLast30Days",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"CoreTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_Phase5_MyEventsKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => UtilityTexts.PtBr,
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "MyEvents.EmptyHint",
            "MyEvents.EmptyHintPoker",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"UtilityTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_Phase5_VenueManagerKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => UtilityTexts.PtBr,
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "VenueManager.ColCity",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"UtilityTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_Phase5_VenueEditKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => UtilityTexts.PtBr,
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "VenueEdit.Loading",
            "VenueEdit.AccessDenied",
            "VenueEdit.Back",
            "VenueEdit.MapsHint",
            "VenueEdit.State",
            "VenueEdit.SelectState",
            "VenueEdit.SelectCity",
            "VenueEdit.AddressPlaceholder",
            "VenueEdit.AddressAutoPlaceholder",
            "VenueEdit.Name",
            "VenueEdit.NamePlaceholder",
            "VenueEdit.Type",
            "VenueEdit.TypeFutsal",
            "VenueEdit.TypePoker",
            "VenueEdit.TypeOther",
            "VenueEdit.Address",
            "VenueEdit.City",
            "VenueEdit.CityPlaceholder",
            "VenueEdit.Capacity",
            "VenueEdit.CapacityPlaceholder",
            "VenueEdit.Active",
            "VenueEdit.ActiveHint",
            "VenueEdit.Save",
            "VenueEdit.Cancel",
            "VenueEdit.Saving",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"UtilityTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_Phase5_DocsIntegrationKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => UtilityTexts.PtBr,
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "Docs.Integration.PageTitle",
            "Docs.Integration.MainTitle",
            "Docs.Integration.Subtitle",
            "Docs.Integration.Intro",
            "Docs.Integration.BadgeAuth",
            "Docs.Integration.BadgeSecure",
            "Docs.Integration.BadgeRate",
            "Docs.Integration.HowToIntegrate",
            "Docs.Integration.Step1Title",
            "Docs.Integration.Step1Desc",
            "Docs.Integration.Step1Sub1",
            "Docs.Integration.Step1Sub2",
            "Docs.Integration.Step1Sub3",
            "Docs.Integration.Step1Tip",
            "Docs.Integration.Step2Title",
            "Docs.Integration.Step2Desc",
            "Docs.Integration.Step2DownloadCanary",
            "Docs.Integration.Step2DownloadTfs",
            "Docs.Integration.Step2Tip",
            "Docs.Integration.Step3Title",
            "Docs.Integration.Step3Desc",
            "Docs.Integration.Step3Tip",
            "Docs.Integration.Step4Title",
            "Docs.Integration.Step4Canary",
            "Docs.Integration.Step4Tfs",
            "Docs.Integration.Step5Title",
            "Docs.Integration.Step5Desc",
            "Docs.Integration.Step5ManualTest",
            "Docs.Integration.Step5Copy",
            "Docs.Integration.Step5Expected",
            "Docs.Integration.EndpointsTitle",
            "Docs.Integration.EndpointsIntro",
            "Docs.Integration.ColMethod",
            "Docs.Integration.ColEndpoint",
            "Docs.Integration.ColDescription",
            "Docs.Integration.EpPing",
            "Docs.Integration.EpTradesPending",
            "Docs.Integration.EpTradesConfirm",
            "Docs.Integration.EpInventoryReport",
            "Docs.Integration.EpOffers",
            "Docs.Integration.NoHttpTitle",
            "Docs.Integration.NoHttpDesc",
            "Docs.Integration.NoHttpOptA",
            "Docs.Integration.NoHttpOptADesc",
            "Docs.Integration.NoHttpOptB",
            "Docs.Integration.NoHttpOptBDesc",
            "Docs.Integration.NoHttpHelp",
            "Docs.Integration.SecurityTitle",
            "Docs.Integration.SecKeyUnique",
            "Docs.Integration.SecKeyUniqueDesc",
            "Docs.Integration.SecHashDb",
            "Docs.Integration.SecHashDbDesc",
            "Docs.Integration.SecRateLimit",
            "Docs.Integration.SecRateLimitDesc",
            "Docs.Integration.SecPerms",
            "Docs.Integration.SecPermsDesc",
            "Docs.Integration.HelpTitle",
            "Docs.Integration.HelpDesc",
            "Docs.Integration.HelpCta",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"UtilityTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    [Theory]
    [InlineData("PtBr")]
    [InlineData("EnUs")]
    [InlineData("EsEs")]
    public void UtilityTexts_Phase5_MailboxKeysExist(string lang)
    {
        var dictionary = lang switch
        {
            "PtBr" => UtilityTexts.PtBr,
            "EnUs" => UtilityTexts.EnUs,
            "EsEs" => UtilityTexts.EsEs,
            _ => throw new ArgumentException($"Unknown language: {lang}")
        };

        var expectedKeys = new[]
        {
            "Mailbox.WorkspaceAria",
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(dictionary.ContainsKey(key),
                $"UtilityTexts.{lang}: missing Phase 5 key '{key}'");
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Values differ across languages (no copy-paste errors)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void CoreTexts_EnUs_DiffersFromPtBr_ForMostKeys()
    {
        var ptBr = CoreTexts.PtBr;
        var enUs = CoreTexts.EnUs;

        var sameValues = ptBr
            .Where(kvp => enUs.TryGetValue(kvp.Key, out var enVal) && enVal == kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        // Some keys may legitimately be the same (e.g. brand names, emojis)
        // but the majority should differ
        Assert.True(sameValues.Count < ptBr.Count / 2,
            $"CoreTexts: too many identical values between PtBr and EnUs ({sameValues.Count}/{ptBr.Count}): {string.Join(", ", sameValues)}");
    }

    [Fact]
    public void UtilityTexts_EnUs_DiffersFromPtBr_ForMostKeys()
    {
        var ptBr = UtilityTexts.PtBr;
        var enUs = UtilityTexts.EnUs;

        var sameValues = ptBr
            .Where(kvp => enUs.TryGetValue(kvp.Key, out var enVal) && enVal == kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        Assert.True(sameValues.Count < ptBr.Count / 2,
            $"UtilityTexts: too many identical values between PtBr and EnUs ({sameValues.Count}/{ptBr.Count}): {string.Join(", ", sameValues)}");
    }
}
