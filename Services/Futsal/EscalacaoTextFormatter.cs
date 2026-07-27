using System.Text;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services.Futsal;

public static class EscalacaoTextFormatter
{
    public static string BuildWhatsAppText(Event ev)
    {
        var sb = new StringBuilder();
        var teamAConfs = ev.Confirmations.Where(c => c.TeamId == 0).OrderBy(c => c.Position).ThenBy(c => c.ConfirmedAt).ToList();
        var teamBConfs = ev.Confirmations.Where(c => c.TeamId == 1).OrderBy(c => c.Position).ThenBy(c => c.ConfirmedAt).ToList();
        var reservaConfs = ev.Confirmations.Where(c => c.TeamId == null).OrderBy(c => c.ConfirmedAt).ToList();

        sb.AppendLine($"*Escalacao — {ev.Group.Name}*");
        sb.AppendLine(ev.StartsAt.ToString("dd/MM/yyyy HH:mm"));
        sb.AppendLine();

        sb.AppendLine("*TIME A*");
        foreach (var c in teamAConfs.Where(c => c.Position == FutsalPosition.Goalkeeper))
            sb.AppendLine($"(Goleiro) {c.User.FullName ?? c.User.Email}");
        int i = 1;
        foreach (var c in teamAConfs.Where(c => c.Position != FutsalPosition.Goalkeeper))
            sb.AppendLine($"{i++}. {c.User.FullName ?? c.User.Email}");
        sb.AppendLine();

        sb.AppendLine("*TIME B*");
        foreach (var c in teamBConfs.Where(c => c.Position == FutsalPosition.Goalkeeper))
            sb.AppendLine($"(Goleiro) {c.User.FullName ?? c.User.Email}");
        i = 1;
        foreach (var c in teamBConfs.Where(c => c.Position != FutsalPosition.Goalkeeper))
            sb.AppendLine($"{i++}. {c.User.FullName ?? c.User.Email}");

        if (reservaConfs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("*Reservas*");
            foreach (var c in reservaConfs)
                sb.AppendLine($"- {c.User.FullName ?? c.User.Email}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildShareText(Event ev)
    {
        var sb = new StringBuilder();
        var teamAConfs = ev.Confirmations.Where(c => c.TeamId == 0).OrderBy(c => c.Position).ThenBy(c => c.ConfirmedAt).ToList();
        var teamBConfs = ev.Confirmations.Where(c => c.TeamId == 1).OrderBy(c => c.Position).ThenBy(c => c.ConfirmedAt).ToList();
        var reservaConfs = ev.Confirmations.Where(c => c.TeamId == null).OrderBy(c => c.ConfirmedAt).ToList();

        sb.AppendLine($"⚽ *Escalacao — {ev.Group.Name}*");
        sb.AppendLine($"📅 {ev.StartsAt.ToString("dd/MM/yyyy HH:mm")}");
        sb.AppendLine();

        sb.AppendLine("*🟡 TIME A*");
        foreach (var c in teamAConfs.Where(c => c.Position == FutsalPosition.Goalkeeper))
            sb.AppendLine($"🧤 {c.User.FullName ?? c.User.Email}");
        int i = 1;
        foreach (var c in teamAConfs.Where(c => c.Position != FutsalPosition.Goalkeeper))
            sb.AppendLine($"{i++}. {c.User.FullName ?? c.User.Email}");
        sb.AppendLine();

        sb.AppendLine("*🔵 TIME B*");
        foreach (var c in teamBConfs.Where(c => c.Position == FutsalPosition.Goalkeeper))
            sb.AppendLine($"🧤 {c.User.FullName ?? c.User.Email}");
        i = 1;
        foreach (var c in teamBConfs.Where(c => c.Position != FutsalPosition.Goalkeeper))
            sb.AppendLine($"{i++}. {c.User.FullName ?? c.User.Email}");

        if (reservaConfs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("🔄 *Reserva*");
            foreach (var c in reservaConfs)
                sb.AppendLine($"• {c.User.FullName ?? c.User.Email}");
        }

        return sb.ToString().TrimEnd();
    }
}
