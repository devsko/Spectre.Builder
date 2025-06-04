// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public abstract class ParallelStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses = null) : CompoundStep(steps, progresses), IStep
{
    protected override Task ExecuteStepsAsync(StepContext context)
    {
        return System.Threading.Tasks.Parallel.ForEachAsync(Steps, ParallelOptions, ExecuteAsync);

        async ValueTask ExecuteAsync(IStep step, CancellationToken cancellationToken)
        {
            await context.ExecuteAsync(step, cancellationToken);
            context.IncrementProgress(this);
        }
    }

    protected abstract ParallelOptions ParallelOptions { get; }
}
