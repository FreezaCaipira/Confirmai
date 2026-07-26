using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Crypto;
using Confirmai.Shared.Helpers;

namespace Confirmai.Services.Payment
{
    /// <summary>
    /// Handles Payment page initialization: query parsing, quote fetching, gateway setup.
    /// Encapsulates logic from OnInitializedAsync to reduce component complexity.
    /// </summary>
    public class PaymentInitializationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly BitcoinQuoteService _bitCoinQuoteService;
        private readonly GatewayService _gatewayService;

        public PaymentInitializationService(IDbContextFactory<AppDbContext> dbFactory, BitcoinQuoteService bitCoinQuoteService, GatewayService gatewayService)
        {
            _dbFactory = dbFactory;
            _bitCoinQuoteService = bitCoinQuoteService;
            _gatewayService = gatewayService;
        }

        /// <summary>
        /// Parses query parameters: qty, maxQty, unitPrice, currency.
        /// </summary>
        public (int Quantity, int MaxQuantity, decimal? UnitPrice, string Currency) ParseQueryParameters(Uri uri)
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            
            int qty = 1;
            if (query.TryGetValue("qty", out var queryQty) && int.TryParse(queryQty.LastOrDefault(), out var parsedQty) && parsedQty > 0)
                qty = parsedQty;

            int maxQty = 1;
            if (query.TryGetValue("maxQty", out var queryMaxQty) && int.TryParse(queryMaxQty.LastOrDefault(), out var parsedMaxQty) && parsedMaxQty > 0)
                maxQty = parsedMaxQty;

            decimal? unitPrice = null;
            if (query.TryGetValue("unitPrice", out var queryUnitPrice)
                && decimal.TryParse(queryUnitPrice.LastOrDefault(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedUnitPrice)
                && parsedUnitPrice > 0m)
                unitPrice = parsedUnitPrice;

            string currency = "BTC";
            if (query.TryGetValue("currency", out var queryCurrency))
            {
                var c = queryCurrency.LastOrDefault()?.ToUpperInvariant();
                if (c == "BRL" || c == "USD" || c == "BTC")
                    currency = c;
            }

            return (qty, maxQty, unitPrice, currency);
        }

        /// <summary>
        /// Fetches Bitcoin exchange rates (USD and BRL).
        /// </summary>
        public async Task<(decimal? BtcUsd, decimal? BtcBrl)> FetchBitcoinRatesAsync()
        {
            var quote = await _bitCoinQuoteService.GetQuoteAsync();
            return (quote?.btc_usd, quote?.btc_brl);
        }

        /// <summary>
        /// Resolves seller information: tries query parameter, falls back to product creator.
        /// </summary>
        public async Task<ApplicationUser?> ResolveSeller(Uri uri, Product? product)
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            
            if (query.TryGetValue("sellerId", out var querySellerIdVal))
            {
                var sellerIdStr = querySellerIdVal.LastOrDefault();
                if (!string.IsNullOrWhiteSpace(sellerIdStr))
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var seller = await db.Users.FirstOrDefaultAsync(u => u.Id == sellerIdStr);
                    if (seller != null)
                        return seller;
                }
            }

            return product?.User;
        }

        /// <summary>
        /// Fetches enabled payment gateways and returns default method preference.
        /// </summary>
        public async Task<(List<GatewayInfo> ActiveGateways, string DefaultMethod)> SetupPaymentGatewaysAsync()
        {
            var gateways = (await _gatewayService.GetAllAsync()).Where(g => g.Enabled).ToList();
            var defaultMethod = gateways.Any(g => g.Name == "Pix")
                ? "Pix"
                : gateways.FirstOrDefault()?.Name ?? "";
            
            return (gateways, defaultMethod);
        }

        /// <summary>
        /// Calculates total purchase amount from unit price and quantity.
        /// </summary>
        public decimal CalculateTotalAmount(decimal unitPrice, int quantity)
        {
            return unitPrice * quantity;
        }

        public async Task<Product?> LoadProductAsync(int productId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Products.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == productId);
        }
    }
}
