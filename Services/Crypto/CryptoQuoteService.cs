using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Confirmai.Data;
using Confirmai.Models;

namespace Confirmai.Services.Crypto
{
    public class CryptoQuoteService
    {
        private readonly HttpClient _http;
        private readonly Dictionary<string, (CryptoQuote? quote, DateTime lastFetch)> _cache;
        private readonly Dictionary<string, DateTime> _lastErrorLogAt;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _errorLogCooldown = TimeSpan.FromMinutes(5);
        private readonly string? _coinGeckoApiKey;
        private readonly string _coinGeckoApiHeaderName;
        private readonly IServiceScopeFactory? _scopeFactory;

        public CryptoQuoteService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IServiceScopeFactory? scopeFactory = null)
        {
            _http = httpClientFactory.CreateClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Confirmai/1.0 (+https://Confirmai.com)");
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _coinGeckoApiKey = configuration["CoinGecko:ApiKey"];
            _coinGeckoApiHeaderName = configuration["CoinGecko:ApiHeaderName"] ?? "x-cg-demo-api-key";
            _scopeFactory = scopeFactory;

            _cache = new Dictionary<string, (CryptoQuote?, DateTime)>();
            _lastErrorLogAt = new Dictionary<string, DateTime>();
        }

        public async Task<CryptoQuote?> GetQuoteAsync(string cryptoId, string symbol, string name)
        {
            if (string.IsNullOrWhiteSpace(cryptoId))
                return null;

            var normalizedCryptoId = cryptoId.Trim().ToLowerInvariant();

            if (_cache.TryGetValue(normalizedCryptoId, out var cached) && 
                cached.quote != null && 
                DateTime.UtcNow - cached.lastFetch < _cacheDuration)
            {
                return cached.quote;
            }

            try
            {
                var encodedId = Uri.EscapeDataString(normalizedCryptoId);
                var url = $"https://api.coingecko.com/api/v3/simple/price?ids={encodedId}&vs_currencies=brl,usd";

                Dictionary<string, CoinGeckoPrice>? result;

                try
                {
                    result = await GetPricesAsync(url, includeApiKey: false);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden && !string.IsNullOrWhiteSpace(_coinGeckoApiKey))
                {
                    result = await GetPricesAsync(url, includeApiKey: true);
                }

                if (result == null || !result.TryGetValue(normalizedCryptoId, out var priceData))
                    return GetCachedQuote(normalizedCryptoId);

                var quote = new CryptoQuote
                {
                    PriceBrl = priceData.Brl,
                    PriceUsd = priceData.Usd,
                    Symbol = symbol,
                    Name = name
                };

                _cache[normalizedCryptoId] = (quote, DateTime.UtcNow);

                return quote;
            }
            catch (Exception ex)
            {
                LogErrorThrottled(normalizedCryptoId, ex);
                return GetCachedQuote(normalizedCryptoId);
            }
        }

        private async Task<Dictionary<string, CoinGeckoPrice>?> GetPricesAsync(string url, bool includeApiKey)
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

            return await response.Content.ReadFromJsonAsync<Dictionary<string, CoinGeckoPrice>>();
        }

        private void LogErrorThrottled(string cryptoId, Exception ex)
        {
            if (_lastErrorLogAt.TryGetValue(cryptoId, out var lastLogAt) && DateTime.UtcNow - lastLogAt < _errorLogCooldown)
                return;

            _lastErrorLogAt[cryptoId] = DateTime.UtcNow;
            Console.WriteLine($"Error loading quote from CoinGecko for '{cryptoId}': {ex.Message}");
        }

        private CryptoQuote? GetCachedQuote(string cryptoId)
        {
            return _cache.TryGetValue(cryptoId, out var cached) ? cached.quote : null;
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
            catch
            {
            }
        }
    }
}



