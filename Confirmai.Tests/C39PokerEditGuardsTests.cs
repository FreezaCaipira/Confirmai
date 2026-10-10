using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C39-A F4 — a edicao do torneio de poker carrega as mesmas guardas do
/// futsal (review C38): Pix do grupo ao subir o valor e bloqueio de
/// buy-in/% quando alguem ja pagou ou enviou comprovante.
/// </summary>
public class C39PokerEditGuardsTests
{
    [Fact]
    public void PokerEdit_ChecaPixAntesDeSalvarNovoValor()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Poker", "Edit.razor.cs"));
        var guard = code.IndexOf("EventPaymentService.GroupCanCharge(ev.Group)", StringComparison.Ordinal);
        var save = code.IndexOf("await db.SaveChangesAsync()", StringComparison.Ordinal);
        Assert.True(guard > 0, "Poker/Edit.razor.cs precisa checar o Pix do grupo ao mudar o buy-in");
        Assert.True(guard < save, "a checagem do Pix precisa vir antes do SaveChanges");
    }

    [Fact]
    public void PokerEdit_CarregaMembrosParaResolverPix()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Poker", "Edit.razor.cs"));
        Assert.Contains("ThenInclude(m => m.User)", code);
    }

    [Fact]
    public void PokerEdit_BloqueiaValorComQuemJaPagou_AntesDeSalvar()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Poker", "Edit.razor.cs"));
        var guard = code.IndexOf("Poker.Edit.PriceLockedPaid", StringComparison.Ordinal);
        var save = code.IndexOf("await db.SaveChangesAsync()", StringComparison.Ordinal);
        Assert.True(guard > 0, "Poker/Edit.razor.cs precisa bloquear buy-in/% com pagamento ou comprovante");
        Assert.True(guard < save, "o bloqueio por pagamento precisa vir antes do SaveChanges");
        Assert.Contains("PixProofUploadedAt", code);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }
}
