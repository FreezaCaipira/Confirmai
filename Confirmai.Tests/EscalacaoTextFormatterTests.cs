using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Futsal;

namespace Confirmai.Tests;

public class EscalacaoTextFormatterTests
{
    private static Event CreateEventWithConfirmations()
    {
        var group = new Group { Id = 1, Name = "Racha Quintas" };
        var ev = new Event
        {
            Id = 10,
            GroupId = 1,
            Group = group,
            StartsAt = new DateTime(2026, 7, 30, 20, 0, 0),
            Sport = Sport.Futsal
        };

        var userA1 = new ApplicationUser { Id = "u1", FullName = "Joao Silva", Email = "joao@test.com" };
        var userA2 = new ApplicationUser { Id = "u2", FullName = "Pedro Santos", Email = "pedro@test.com" };
        var userBGk = new ApplicationUser { Id = "u3", FullName = "Maria Costa", Email = "maria@test.com" };
        var userB1 = new ApplicationUser { Id = "u4", FullName = "Ana Lima", Email = "ana@test.com" };
        var userRes = new ApplicationUser { Id = "u5", FullName = "Carlos Souza", Email = "carlos@test.com" };

        ev.Confirmations = new List<EventConfirmation>
        {
            new() { Id = 1, EventId = 10, UserId = "u1", User = userA1, TeamId = 0, Position = FutsalPosition.Outfield, ConfirmedAt = new DateTime(2026, 7, 28, 10, 0, 0) },
            new() { Id = 2, EventId = 10, UserId = "u2", User = userA2, TeamId = 0, Position = FutsalPosition.Goalkeeper, ConfirmedAt = new DateTime(2026, 7, 28, 9, 0, 0) },
            new() { Id = 3, EventId = 10, UserId = "u3", User = userBGk, TeamId = 1, Position = FutsalPosition.Goalkeeper, ConfirmedAt = new DateTime(2026, 7, 28, 9, 30, 0) },
            new() { Id = 4, EventId = 10, UserId = "u4", User = userB1, TeamId = 1, Position = FutsalPosition.Outfield, ConfirmedAt = new DateTime(2026, 7, 28, 11, 0, 0) },
            new() { Id = 5, EventId = 10, UserId = "u5", User = userRes, TeamId = null, Position = FutsalPosition.Outfield, ConfirmedAt = new DateTime(2026, 7, 28, 12, 0, 0) },
        };

        return ev;
    }

    private static Event CreateEmptyEvent()
    {
        return new Event
        {
            Id = 1,
            GroupId = 1,
            Group = new Group { Id = 1, Name = "Grupo Vazio" },
            StartsAt = new DateTime(2026, 7, 30, 20, 0, 0),
            Sport = Sport.Futsal,
            Confirmations = new List<EventConfirmation>()
        };
    }

    [Fact]
    public void BuildWhatsAppText_ReturnsNonEmpty_WithConfirmations()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.NotEmpty(result);
        Assert.Contains("Racha Quintas", result);
        Assert.Contains("30/07/2026 20:00", result);
    }

    [Fact]
    public void BuildWhatsAppText_ContainsBothTeamLabels()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.Contains("TIME A", result);
        Assert.Contains("TIME B", result);
    }

    [Fact]
    public void BuildWhatsAppText_ContainsGoalkeeperLabel()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.Contains("(Goleiro)", result);
    }

    [Fact]
    public void BuildWhatsAppText_ContainsReservasSection_WhenReservesExist()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.Contains("Reservas", result);
        Assert.Contains("Carlos Souza", result);
    }

    [Fact]
    public void BuildWhatsAppText_OmitsReservasSection_WhenNoReserves()
    {
        var ev = CreateEventWithConfirmations();
        ev.Confirmations = ev.Confirmations.Where(c => c.TeamId != null).ToList();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.DoesNotContain("Reservas", result);
    }

    [Fact]
    public void BuildWhatsAppText_FallsBackToEmail_WhenFullNameIsNull()
    {
        var ev = CreateEmptyEvent();
        ev.Confirmations = new List<EventConfirmation>
        {
            new() { Id = 1, EventId = 1, UserId = "u1", User = new ApplicationUser { Id = "u1", FullName = null, Email = "fallback@test.com" }, TeamId = 0, Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow }
        };

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.Contains("fallback@test.com", result);
    }

    [Fact]
    public void BuildWhatsAppText_ReturnsMinimal_WithEmptyConfirmations()
    {
        var ev = CreateEmptyEvent();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        Assert.NotEmpty(result);
        Assert.Contains("Grupo Vazio", result);
        Assert.Contains("TIME A", result);
        Assert.Contains("TIME B", result);
    }

    [Fact]
    public void BuildWhatsAppText_GoalkeeperListedBeforeOutfield()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildWhatsAppText(ev);

        var gkIdx = result.IndexOf("(Goleiro)");
        var outfieldIdx = result.IndexOf("1. Joao Silva");
        Assert.True(gkIdx >= 0 && outfieldIdx >= 0);
        Assert.True(gkIdx < outfieldIdx);
    }

    [Fact]
    public void BuildShareText_ReturnsNonEmpty_WithConfirmations()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.NotEmpty(result);
        Assert.Contains("Racha Quintas", result);
        Assert.Contains("30/07/2026 20:00", result);
    }

    [Fact]
    public void BuildShareText_ContainsBothTeamLabels()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.Contains("TIME A", result);
        Assert.Contains("TIME B", result);
    }

    [Fact]
    public void BuildShareText_ContainsGoalkeeperName()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.Contains("Pedro Santos", result);
    }

    [Fact]
    public void BuildShareText_ContainsReservaSection_WhenReservesExist()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.Contains("Reserva", result);
        Assert.Contains("Carlos Souza", result);
    }

    [Fact]
    public void BuildShareText_OmitsReservaSection_WhenNoReserves()
    {
        var ev = CreateEventWithConfirmations();
        ev.Confirmations = ev.Confirmations.Where(c => c.TeamId != null).ToList();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.DoesNotContain("Reserva", result);
    }

    [Fact]
    public void BuildShareText_FallsBackToEmail_WhenFullNameIsNull()
    {
        var ev = CreateEmptyEvent();
        ev.Confirmations = new List<EventConfirmation>
        {
            new() { Id = 1, EventId = 1, UserId = "u1", User = new ApplicationUser { Id = "u1", FullName = null, Email = "fallback@test.com" }, TeamId = 0, Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow }
        };

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.Contains("fallback@test.com", result);
    }

    [Fact]
    public void BuildShareText_ReturnsMinimal_WithEmptyConfirmations()
    {
        var ev = CreateEmptyEvent();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.NotEmpty(result);
        Assert.Contains("Grupo Vazio", result);
        Assert.Contains("TIME A", result);
        Assert.Contains("TIME B", result);
    }

    [Fact]
    public void BuildShareText_GoalkeeperListedBeforeOutfield()
    {
        var ev = CreateEventWithConfirmations();

        var result = EscalacaoTextFormatter.BuildShareText(ev);

        var gkIdx = result.IndexOf("Pedro Santos");
        var outfieldIdx = result.IndexOf("1. Joao Silva");
        Assert.True(gkIdx >= 0 && outfieldIdx >= 0);
        Assert.True(gkIdx < outfieldIdx);
    }

    [Fact]
    public void BothMethods_ReturnDifferentFormats_ForSameEvent()
    {
        var ev = CreateEventWithConfirmations();

        var whatsapp = EscalacaoTextFormatter.BuildWhatsAppText(ev);
        var share = EscalacaoTextFormatter.BuildShareText(ev);

        Assert.NotEqual(whatsapp, share);
    }
}
