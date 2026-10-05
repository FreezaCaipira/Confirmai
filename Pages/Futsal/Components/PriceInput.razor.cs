using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class PriceInput
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();

    /// <summary>Trava o valor em 0 quando o grupo nao tem recebedor Pix (C38 F5).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Sobrescreve o hint padrao (ex.: "Partida gratuita — cadastre o Pix").</summary>
    [Parameter]
    public string? Hint { get; set; }
}
