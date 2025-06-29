
namespace Spectre.Builder.Tests;

public static class SmokeTest
{
    [Fact]
    public static async Task Run()
    {
        await new Context(default).RunAsync(new Step1(), [new MemoryInfo<Context>()]);
    }

    private class Context(CancellationToken cancellationToken) : BuilderContext<Context>(cancellationToken)
    { }

    private class Step1() : ConversionStep<Context>([], [])
    {
        protected override Task ExecuteAsync(Context builderContext, DateTime timestamp, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

