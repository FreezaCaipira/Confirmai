using System.Reflection;

namespace Confirmai.Tests;

public class AdminAuthorizationConventionsTests
{
    [Fact]
    public void AdminRazorPages_MustDeclareAdminRoleAuthorization()
    {
        var solutionRoot = ResolveSolutionRoot();
        var adminPagesPath = Path.Combine(solutionRoot, "Pages", "Admin");

        Assert.True(Directory.Exists(adminPagesPath), $"Admin pages directory not found: {adminPagesPath}");

        var razorFiles = Directory
            .GetFiles(adminPagesPath, "*.razor", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path)
            .ToArray();

        Assert.NotEmpty(razorFiles);

        foreach (var razorFile in razorFiles)
        {
            var content = File.ReadAllText(razorFile);

            Assert.Contains("@page \"/admin", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@attribute [Authorize(Roles = \"admin\")]", content, StringComparison.Ordinal);
        }
    }

    private static string ResolveSolutionRoot()
    {
        var testAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Could not resolve test assembly location.");

        var root = Path.GetFullPath(Path.Combine(testAssemblyLocation, "..", "..", "..", ".."));
        return root;
    }
}


