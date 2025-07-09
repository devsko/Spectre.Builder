
namespace Spectre.Builder.Tests;

public static class SmokeTest
{
    [Fact]
    public static async Task Run()
    {
        await new Context().RunAsync(new Step1(), [new MemoryInfo<Context>()], TestContext.Current.CancellationToken);
    }

    private class Context : BuilderContext<Context>
    { }

    private class Step1() : ConversionStep<Context>([], [])
    {
        protected override Task ExecuteAsync(Context builderContext, DateTime timestamp, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

