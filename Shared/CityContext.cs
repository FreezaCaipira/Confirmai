namespace Confirmai.Shared;

/// <summary>
/// Contexto de cidade selecionada para uso com CascadingValue.
/// Permite que componentes filhos acessem a cidade e estado selecionados
/// sem precisar passá-los explicitamente como parâmetros.
/// </summary>
public record CityContext(string? City, string? StateCode)
{
    /// <summary>
    /// Retorna true se uma cidade específica foi selecionada (não é "Outra cidade").
    /// </summary>
    public bool HasCity => !string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(StateCode);
}
