using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class PartidasDateTooltipIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PartidasDateTooltipIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Partidas_RendersDateTooltip_WithWeekdayAndRelative()
    {
        const string adminUserId = "tooltip-admin-1";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Racha Tooltip");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Events.Add(new Event
            {
                GroupId         = groupId,
                Sport           = Sport.Futsal,
                Location        = "Quadra do Bairro",
                StartsAt        = DateTime.UtcNow.AddDays(1),
                MaxPlayers      = 12,
                MaxGoalkeepers  = 2,
                IsActive        = true,
                CreatedByUserId = adminUserId,
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync($"/grupo/{groupId}/partidas");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Accessible tooltip wired to the date
        Assert.Contains("role=\"tooltip\"", html);
        Assert.Contains("aria-describedby=\"et-tip-", html);
        // Weekday always visible in the date cell: "ddd dd/MM" -> e.g. "seg. 23/06".
        // Accented weekdays ("sáb.") arrive HTML-encoded ("s&#xE1;b"), so the
        // weekday part is matched loosely — only the trailing dd/MM is strict.
        Assert.Matches("class=\"et-date\"[^>]*>[^<]*\\d{2}/\\d{2}", html);
        // Tooltip carries the long date + relative line + venue
        Assert.Contains("et-tip-relative", html);
        Assert.Contains("Quadra do Bairro", html);
    }
}
