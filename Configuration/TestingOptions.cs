namespace Confirmai.Configuration;

/// <summary>
/// C43-B — preenchimento de partida com jogadores de teste.
/// Env: Testing__MockFillEnabled (default false; ligar so em staging/prod
/// controlado, e remover no checklist de reset).
/// </summary>
public sealed class TestingOptions
{
    public const string Section = "Testing";

    public bool MockFillEnabled { get; set; } = false;
}
