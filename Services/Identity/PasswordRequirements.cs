using Confirmai.Configuration;
using Confirmai.Services.Core;

namespace Confirmai.Services.Identity;

/// <summary>
/// C38 F1 — monta a lista de requisitos de senha visivel na tela a partir da
/// SecurityPolicySnapshot ativa (dev e prod tem regras diferentes). Cada item
/// tem um RuleId que o identity-register.js avalia enquanto o usuario digita.
/// </summary>
public static class PasswordRequirements
{
    public sealed record Item(string RuleId, string Text);

    public static IReadOnlyList<Item> Build(SecurityPolicySnapshot policy, UiTextService ui)
    {
        var items = new List<Item>
        {
            new($"minLength:{policy.PasswordRequiredLength}",
                ui.Get("Identity.PasswordRule.MinLength", policy.PasswordRequiredLength.ToString())),
        };

        if (policy.PasswordRequireUppercase)
            items.Add(new("upper", ui["Identity.PasswordRule.Uppercase"]));
        if (policy.PasswordRequireLowercase)
            items.Add(new("lower", ui["Identity.PasswordRule.Lowercase"]));
        if (policy.PasswordRequireDigit)
            items.Add(new("digit", ui["Identity.PasswordRule.Digit"]));
        if (policy.PasswordRequireNonAlphanumeric)
            items.Add(new("symbol", ui["Identity.PasswordRule.Symbol"]));
        if (policy.PasswordRequiredUniqueChars > 1)
            items.Add(new($"unique:{policy.PasswordRequiredUniqueChars}",
                ui.Get("Identity.PasswordRule.UniqueChars", policy.PasswordRequiredUniqueChars.ToString())));

        return items;
    }
}
