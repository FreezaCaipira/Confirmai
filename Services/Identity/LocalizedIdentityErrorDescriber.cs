using Confirmai.Services.Core;
using Microsoft.AspNetCore.Identity;

namespace Confirmai.Services.Identity;

/// <summary>
/// C38 F1 — traduz os erros padrao do Identity (senha fora da politica, email
/// duplicado, token invalido) para o idioma da UI. Sem isso as mensagens saem
/// em ingles para o usuario final.
/// </summary>
public sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly UiTextService _ui;

    public LocalizedIdentityErrorDescriber(UiTextService ui) => _ui = ui;

    private IdentityError Err(string key) =>
        new() { Code = key[(key.LastIndexOf('.') + 1)..], Description = _ui[key] };

    private IdentityError Err(string key, params object[] args) =>
        new() { Code = key[(key.LastIndexOf('.') + 1)..], Description = _ui.Get(key, args) };

    public override IdentityError DefaultError() => Err("Identity.Error.Default");
    public override IdentityError PasswordTooShort(int length) => Err("Identity.Error.PasswordTooShort", length);
    public override IdentityError PasswordRequiresDigit() => Err("Identity.Error.PasswordRequiresDigit");
    public override IdentityError PasswordRequiresLower() => Err("Identity.Error.PasswordRequiresLower");
    public override IdentityError PasswordRequiresUpper() => Err("Identity.Error.PasswordRequiresUpper");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Err("Identity.Error.PasswordRequiresNonAlphanumeric");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Err("Identity.Error.PasswordRequiresUniqueChars", uniqueChars);
    public override IdentityError DuplicateUserName(string userName) => Err("Identity.Error.DuplicateUserName");
    public override IdentityError DuplicateEmail(string email) => Err("Identity.Error.DuplicateEmail");
    public override IdentityError InvalidEmail(string? email) => Err("Identity.Error.InvalidEmail");
    public override IdentityError InvalidToken() => Err("Identity.Error.InvalidToken");
    public override IdentityError PasswordMismatch() => Err("Identity.Error.PasswordMismatch");
}
