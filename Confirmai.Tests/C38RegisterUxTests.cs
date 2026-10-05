using System.ComponentModel.DataAnnotations;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Identity;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C38 F1 — cadastro que nao frustra: requisitos de senha visiveis gerados da
/// politica ativa, erros do Identity/DataAnnotations traduzidos, script como
/// arquivo (CSP).
/// </summary>
public class C38RegisterUxTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C38RegisterUxTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    private static UiTextService Ui(string lang)
    {
        var pref = new LanguagePreferenceService();
        pref.SetLanguage(lang);
        return new UiTextService(pref);
    }

    // (1) Requisitos renderizados batem com a politica dev e prod.
    [Fact]
    public void PasswordRequirements_DevPolicy_Min6NoSymbol()
    {
        var items = PasswordRequirements.Build(SecurityPolicyDefaults.Create(true), Ui("pt-BR"));

        Assert.Contains(items, i => i.RuleId == "minLength:6" && i.Text.Contains("6"));
        Assert.Contains(items, i => i.RuleId == "digit");
        Assert.Contains(items, i => i.RuleId == "lower");
        Assert.DoesNotContain(items, i => i.RuleId == "upper");
        Assert.DoesNotContain(items, i => i.RuleId == "symbol");
        Assert.DoesNotContain(items, i => i.RuleId.StartsWith("unique"));
    }

    [Fact]
    public void PasswordRequirements_ProdPolicy_Min10AllRules()
    {
        var items = PasswordRequirements.Build(SecurityPolicyDefaults.Create(false), Ui("pt-BR"));

        Assert.Contains(items, i => i.RuleId == "minLength:10");
        Assert.Contains(items, i => i.RuleId == "upper");
        Assert.Contains(items, i => i.RuleId == "lower");
        Assert.Contains(items, i => i.RuleId == "digit");
        Assert.Contains(items, i => i.RuleId == "symbol");
        Assert.Contains(items, i => i.RuleId == "unique:3");
    }

    // (3) Describer traduz os codigos nas 3 linguas.
    [Theory]
    [InlineData("pt-BR", "senha")]
    [InlineData("en-US", "password")]
    [InlineData("es-ES", "contraseña")]
    public void Describer_TranslatesPasswordErrors_InAllLanguages(string lang, string marker)
    {
        var describer = new LocalizedIdentityErrorDescriber(Ui(lang));

        Assert.Contains(marker, describer.PasswordTooShort(10).Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("PasswordTooShort", describer.PasswordTooShort(10).Code);
        Assert.DoesNotContain("Identity.", describer.PasswordRequiresUpper().Description);
        Assert.DoesNotContain("Identity.", describer.DuplicateEmail("a@b.c").Description);
        Assert.DoesNotContain("Identity.", describer.InvalidToken().Description);
    }

    // (2) POST com senhas diferentes devolve mensagem traduzida e mantem email.
    [Fact]
    public async Task Register_MismatchedPasswords_LocalizedError_KeepsEmail()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var get = await client.GetAsync("/Identity/Account/Register?uiLang=pt-BR");
        var html = await get.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex
            .Match(html, "<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")
            .Groups["token"].Value;
        Assert.False(string.IsNullOrEmpty(token));

        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("__RequestVerificationToken", token),
            KeyValuePair.Create("Input.Email", "c38-mismatch@test.local"),
            KeyValuePair.Create("Input.Password", "SenhaForte1!"),
            KeyValuePair.Create("Input.ConfirmPassword", "OutraSenha2@"),
        });

        var response = await client.PostAsync("/Identity/Account/Register?uiLang=pt-BR", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("senhas", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("do not match", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c38-mismatch@test.local", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Email == "c38-mismatch@test.local"));
    }

    // (4) Script referenciado como arquivo; sem <script> inline na pagina.
    [Fact]
    public async Task Register_ReferencesStaticJs_NoInlineScript()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Identity/Account/Register"))
            .Content.ReadAsStringAsync();

        Assert.Contains("/js/identity-register.js", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("data-req=\"minLength:", html);
    }
}
