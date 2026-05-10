namespace Confirmai.Services;

public class CurrencyPreferenceService
{
    private string _selectedFiatCurrency = "USD";

    public string SelectedFiatCurrency => _selectedFiatCurrency;

    public void SetCurrency(string? currency)
    {
        if (string.Equals(currency, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            _selectedFiatCurrency = "BRL";
            return;
        }

        _selectedFiatCurrency = "USD";
    }
}

