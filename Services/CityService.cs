using Confirmai.Data;
using Confirmai.Shared;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

/// <summary>
/// Service para operações relacionadas a cidades.
/// </summary>
public class CityService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CityService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Carrega todas as opções de cidades ativas do banco de dados.
    /// </summary>
    public async Task<List<CityOption>> GetCityOptionsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.Groups
            .Where(g => g.IsActive)
            .Select(g => new { g.City, g.StateCode })
            .Distinct()
            .OrderBy(c => c.City)
            .ToListAsync();

        return rows
            .Select(r => new CityOption(
                $"{r.City}|{r.StateCode}",
                $"{r.City} - {r.StateCode}",
                r.City,
                r.StateCode))
            .ToList();
    }

    /// <summary>
    /// Carrega municípios IBGE para um estado específico.
    /// </summary>
    public async Task<List<IbgeMunicipio>> GetIbgeMunicipiosAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return new List<IbgeMunicipio>();

        try
        {
            var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            var url = $"https://servicodados.ibge.gov.br/api/v1/localidades/estados/{stateCode}/municipios";
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<IbgeMunicipio>>(json) ?? new List<IbgeMunicipio>();
        }
        catch
        {
            return new List<IbgeMunicipio>();
        }
    }
}
