using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Confirmai.Services.Core;

namespace Confirmai.Pages.Groups.Components;

public partial class CityAutocomplete
{
    /// <summary>Cidades oficiais do UF selecionado (cities.json).</summary>
    [Parameter]
    public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private readonly string _id = Guid.NewGuid().ToString("N")[..8];
    private string text = string.Empty;
    private List<string> suggestions = new();
    private int activeIndex = -1;
    private bool isOpen;

    private string ListId => $"city-list-{_id}";
    private string OptionId(int index) => $"city-opt-{_id}-{index}";
    private string? ActiveDescendantId => activeIndex >= 0 ? OptionId(activeIndex) : null;

    protected override void OnParametersSet()
    {
        if (Value != text)
            text = Value ?? string.Empty;
    }

    private async Task OnInput(ChangeEventArgs e)
    {
        text = e.Value?.ToString() ?? string.Empty;
        await ValueChanged.InvokeAsync(text);

        suggestions = CitySearch.Search(Cities, text);
        activeIndex = -1;
        isOpen = suggestions.Count > 0;
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (suggestions.Count == 0) return;
                activeIndex = (activeIndex + 1) % suggestions.Count;
                isOpen = true;
                break;
            case "ArrowUp":
                if (suggestions.Count == 0) return;
                activeIndex = activeIndex <= 0 ? suggestions.Count - 1 : activeIndex - 1;
                break;
            case "Enter":
                if (isOpen && activeIndex >= 0)
                    await Select(suggestions[activeIndex]);
                break;
            case "Escape":
                isOpen = false;
                activeIndex = -1;
                break;
        }
    }

    private void OnBlur()
    {
        // onmousedown + preventDefault nas opcoes garante a selecao antes do blur.
        isOpen = false;
        activeIndex = -1;
    }

    private async Task Select(string city)
    {
        text = city;
        isOpen = false;
        activeIndex = -1;
        suggestions = new();
        await ValueChanged.InvokeAsync(city);
    }
}
