using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Review C38 — a edicao da partida de futsal nao pode virar atalho para
/// cobrar sem recebedor Pix (a criacao trava o valor em 0 sem Pix).
/// </summary>
public class C38ReviewFutsalEditPixTests
{
    private static Group GroupWith(bool gateways, string? receiverId, params (string id, GroupMemberRole role, string? pix)[] members)
    {
        var group = new Group { Id = 1, Name = "G", EnablePaymentGateways = gateways, PixReceiverUserId = receiverId };
        var t = DateTime.UtcNow;
        foreach (var (id, role, pix) in members)
        {
            group.Members.Add(new GroupMember
            {
                UserId = id,
                Role = role,
                CreatedAt = t = t.AddMinutes(1),
                User = new ApplicationUser { Id = id, PixKey = pix },
            });
        }
        return group;
    }

    [Fact]
    public void GroupCanCharge_SemPixESemGateway_False()
        => Assert.False(EventPaymentService.GroupCanCharge(
            GroupWith(false, null, ("a", GroupMemberRole.Admin, null), ("m", GroupMemberRole.Member, "m@pix"))));

    [Fact]
    public void GroupCanCharge_CoAdminComPix_True()
        => Assert.True(EventPaymentService.GroupCanCharge(
            GroupWith(false, null, ("a", GroupMemberRole.Admin, null), ("b", GroupMemberRole.Admin, "b@pix"))));

    [Fact]
    public void GroupCanCharge_RecebedorEscolhidoComPix_True()
        => Assert.True(EventPaymentService.GroupCanCharge(
            GroupWith(false, "m", ("a", GroupMemberRole.Admin, null), ("m", GroupMemberRole.Member, "m@pix"))));

    [Fact]
    public void GroupCanCharge_GatewayLigado_True()
        => Assert.True(EventPaymentService.GroupCanCharge(
            GroupWith(true, null, ("a", GroupMemberRole.Admin, null))));

    [Fact]
    public void FutsalEdit_ChecaPixAntesDeSalvarNovoValor()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Futsal", "Edit.razor.cs"));
        var guard = code.IndexOf("EventPaymentService.GroupCanCharge(ev.Group)", StringComparison.Ordinal);
        var save = code.IndexOf("await db.SaveChangesAsync()", StringComparison.Ordinal);
        Assert.True(guard > 0, "Edit.razor.cs precisa checar o Pix do grupo ao mudar o valor");
        Assert.True(guard < save, "a checagem do Pix precisa vir antes do SaveChanges");
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
