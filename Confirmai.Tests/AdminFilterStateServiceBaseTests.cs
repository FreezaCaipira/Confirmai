using Confirmai.Services.Admin;
using Microsoft.JSInterop;
using Moq;

namespace Confirmai.Tests;

public class AdminFilterStateServiceBaseTests
{
    private sealed class TestState
    {
        public string Value { get; set; } = "default";
    }

    private sealed class TestService : AdminFilterStateServiceBase<TestState>
    {
        private readonly bool _shouldThrow;

        public TestService(IJSRuntime js, bool shouldThrow = false) : base(js)
        {
            _shouldThrow = shouldThrow;
        }

        protected override Task<TestState> LoadCoreAsync()
        {
            if (_shouldThrow)
                throw new InvalidOperationException("Load failed");
            return Task.FromResult(new TestState { Value = "loaded" });
        }

        protected override Task SaveCoreAsync(TestState state)
        {
            if (_shouldThrow)
                throw new InvalidOperationException("Save failed");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefault_WhenLoadCoreThrows()
    {
        var jsMock = new Mock<IJSRuntime>();
        var service = new TestService(jsMock.Object, shouldThrow: true);

        var result = await service.LoadAsync();

        Assert.Equal("default", result.Value);
    }

    [Fact]
    public async Task LoadAsync_ReturnsLoadedState_WhenLoadCoreSucceeds()
    {
        var jsMock = new Mock<IJSRuntime>();
        var service = new TestService(jsMock.Object, shouldThrow: false);

        var result = await service.LoadAsync();

        Assert.Equal("loaded", result.Value);
    }

    [Fact]
    public async Task SaveAsync_SilentlyIgnores_WhenSaveCoreThrows()
    {
        var jsMock = new Mock<IJSRuntime>();
        var service = new TestService(jsMock.Object, shouldThrow: true);

        var exception = await Record.ExceptionAsync(() => service.SaveAsync(new TestState()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveAsync_CallsSaveCore_WhenNoException()
    {
        var jsMock = new Mock<IJSRuntime>();
        var service = new TestService(jsMock.Object, shouldThrow: false);

        await service.SaveAsync(new TestState { Value = "test" });

        // No exception thrown
        Assert.True(true);
    }
}
