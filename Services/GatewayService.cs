using System.Collections.Generic;
using System.Threading.Tasks;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Confirmai.Services
{
    public class GatewayService : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public GatewayService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<GatewayInfo>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var defaults = new[]
            {
                new { Name = "Pix",          Enabled = true  },
                new { Name = "EfiBank",      Enabled = true  },
                new { Name = "Testnet",      Enabled = true  },
                new { Name = "BTCPayServer", Enabled = false },
            };

            var existing = await db.Gateways
                .AsNoTracking()
                .Select(g => g.Name)
                .ToListAsync();

            var existingSet = existing.ToHashSet(System.StringComparer.OrdinalIgnoreCase);
            var hasChanges = false;

            foreach (var gateway in defaults)
            {
                if (existingSet.Contains(gateway.Name))
                {
                    continue;
                }

                db.Gateways.Add(new GatewayInfo
                {
                    Name = gateway.Name,
                    Enabled = gateway.Enabled
                });

                hasChanges = true;
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync();
            }

            return await db.Gateways
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<bool> SetStatusAsync(string name, bool enabled)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var gateway = await db.Gateways.FindAsync(name);
            if (gateway == null) return false;
            gateway.Enabled = enabled;
            await db.SaveChangesAsync();
            return true;
        }
    }
}

