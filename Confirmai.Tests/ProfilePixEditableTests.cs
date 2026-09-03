using System.Reflection;

namespace Confirmai.Tests;

/// <summary>
/// The #pix block on the own profile must let the user type and save a Pix key.
/// Without it, manual Pix payment (V1) can never be enabled for a group.
/// </summary>
public class ProfilePixEditableTests
{
    [Fact]
    public void ProfilePixSection_BindsPixKeyInputAndSaveAction()
    {
        var root = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", "..", ".."));
        var content = File.ReadAllText(Path.Combine(root, "Pages", "Profile.razor"));

        var pixSection = content[content.IndexOf("id=\"pix\"", StringComparison.Ordinal)..];
        pixSection = pixSection[..pixSection.IndexOf("</section>", StringComparison.Ordinal)];

        Assert.Contains("@bind=\"profileEditModel.PixKey\"", pixSection, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"SaveOwnProfileCallback\"", pixSection, StringComparison.Ordinal);
    }
}
