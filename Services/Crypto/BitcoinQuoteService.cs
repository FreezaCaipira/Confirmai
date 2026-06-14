using System.Net.Http.Json;
using System.Net.Http.Headers;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Crypto
{
    public class BitcoinQuoteService
    {
        private readonly HttpClient _http;
        private readonly string? _coinGeckoApiKey;
        private readonly string _coinGeckoApiHeaderName;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly ILogger<BitcoinQuoteService> _logger;

        public BitcoinQuoteService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IServiceScopeFactory? scopeFactory = null, ILogger<BitcoinQuoteService>? logger = null)
        {
            _http = httpClientFactory.CreateClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Confirmai/1.0 (+https://Confirmai.com)");
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _coinGeckoApiKey = configuration["CoinGecko:ApiKey"];
            _coinGeckoApiHeaderName = configuration["CoinGecko:ApiHeaderName"] ?? "x-cg-demo-api-key";
            _scopeFactory = scopeFactory;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BitcoinQuoteService>.Instance;
        }

        private BitcoinQuote? _cachedQuote;
        private DateTime _lastFetch = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(2);

        public async Task<BitcoinQuote?> GetQuoteAsync()
        {
            if (_cachedQuote != null && DateTime.UtcNow - _lastFetch < _cacheDuration)
                return _cachedQuote;

            try
            {
                var url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=brl,usd";

                CoinGeckoResponse? result;
                try
                {
                    result = await GetQuoteResponseAsync(url, includeApiKey: false);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden && !string.IsNullOrWhiteSpace(_coinGeckoApiKey))
                {
                    result = await GetQuoteResponseAsync(url, includeApiKey: true);
                }

                if (result?.bitcoin == null)
                    return null;

                _cachedQuote = new BitcoinQuote
                {
                    btc_brl = result.bitcoin.Brl,
                    btc_usd = result.bitcoin.Usd
                };

                _lastFetch = DateTime.UtcNow;

                return _cachedQuote;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao obter cotação Bitcoin do CoinGecko");
                return _cachedQuote;
            }
        }

        private async Task<CoinGeckoResponse?> GetQuoteResponseAsync(string url, bool includeApiKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri("https://www.coingecko.com/");

            if (includeApiKey && !string.IsNullOrWhiteSpace(_coinGeckoApiKey))
            {
                request.Headers.Remove(_coinGeckoApiHeaderName);
                request.Headers.Add(_coinGeckoApiHeaderName, _coinGeckoApiKey);
            }

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CoinGeckoResponse>();
        }

        private async Task TrackQuoteQueryAsync(string provider, string asset)
        {
            if (_scopeFactory is null)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetService<AppDbContext>();
                if (db is null)
                    return;

                db.Logs.Add(new AppLog
                {
                    Source = "QuoteQuery",
                    Level = "Info",
                    Message = $"{provider}:{asset}"
                });

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Falha ao registrar consulta de cotação (não-crítico)");
            }
        }
    }
}


