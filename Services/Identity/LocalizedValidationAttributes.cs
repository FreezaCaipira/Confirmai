using System.ComponentModel.DataAnnotations;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Confirmai.Services.Identity;

/// <summary>
/// C38 F1 — os atributos de validacao das telas Identity carregam a CHAVE i18n
/// como ErrorMessage (ex.: "Identity.Validation.PasswordMismatch"); o
/// ValidationContext do binding nao expõe servicos, entao a traducao acontece
/// no POST via <see cref="ModelStateLocalizer.Translate"/>, que tambem expande
/// "Chave|arg1|arg2" para mensagens com placeholder.
/// </summary>
public static class ModelStateLocalizer
{
    public static void Translate(ModelStateDictionary modelState, UiTextService ui)
    {
        foreach (var entry in modelState.Values)
        {
            for (var i = 0; i < entry.Errors.Count; i++)
            {
                var message = entry.Errors[i].ErrorMessage;
                if (!message.StartsWith("Identity.", StringComparison.Ordinal))
                    continue;

                var parts = message.Split('|');
                var text = parts.Length > 1 ? ui.Get(parts[0], parts.Skip(1).Cast<object>().ToArray()) : ui[parts[0]];
                entry.Errors[i] = entry.Errors[i].Exception is { } ex
                    ? new ModelError(ex, text)
                    : new ModelError(text);
            }
        }
    }
}

/// <summary>StringLength que emite a chave com min|max para o localizer.</summary>
public sealed class LocalizedStringLengthAttribute : StringLengthAttribute
{
    public LocalizedStringLengthAttribute(int maximumLength) : base(maximumLength) { }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (base.IsValid(value, context) == ValidationResult.Success)
            return ValidationResult.Success;
        return new ValidationResult(
            $"Identity.Validation.StringLength|{MinimumLength}|{MaximumLength}",
            context.MemberName is null ? null : new[] { context.MemberName });
    }
}
