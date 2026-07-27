using System.Text;
using Confirmai.Models;
using Confirmai.Services.Core;

namespace Confirmai.Services.Payment;

public static class PaymentsHistoryExportHelper
{
    public static string BuildCsv(IEnumerable<PaymentRecord> payments, Func<PaymentRecord, string> productName, Func<PaymentRecord, string> formatAmount, UiTextService T)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Data,Produto,Valor,Status,Gateway");
        foreach (var p in payments)
        {
            var status = p.IsPaid ? T["Common.Paid"] : T["Common.Pending"];
            var product = productName(p);
            var amount = formatAmount(p);
            var date = p.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var gateway = p.PaymentMethod ?? "-";
            csv.AppendLine($"\"{date}\",\"{product}\",\"{amount}\",\"{status}\",\"{gateway}\"");
        }
        return csv.ToString();
    }

    public static string BuildHtml(IEnumerable<PaymentRecord> payments, Func<PaymentRecord, string> productName, Func<PaymentRecord, string> formatAmount, UiTextService T)
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; }
        h1 { color: #333; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f4f4f4; }
        .paid { color: green; font-weight: bold; }
        .pending { color: orange; font-weight: bold; }
    </style>
</head>
<body>
    <h1>Histórico de Pagamentos</h1>
    <p>Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @"</p>
    <table>
        <thead>
            <tr>
                <th>Data</th>
                <th>Produto</th>
                <th>Valor</th>
                <th>Status</th>
                <th>Gateway</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var p in payments)
        {
            var status = p.IsPaid ? T["Common.Paid"] : T["Common.Pending"];
            var statusClass = p.IsPaid ? "paid" : "pending";
            var product = productName(p);
            var amount = formatAmount(p);
            var date = p.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var gateway = p.PaymentMethod ?? "-";
            html += $@"
            <tr>
                <td>{date}</td>
                <td>{product}</td>
                <td>{amount}</td>
                <td class='{statusClass}'>{status}</td>
                <td>{gateway}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
</body>
</html>";
        return html;
    }
}
