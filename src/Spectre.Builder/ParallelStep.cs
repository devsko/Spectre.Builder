// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a compound step that executes its child steps in parallel.
/// </summary>
public abstract class ParallelStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses = null) : CompoundStep(steps, progresses), IStep
{
    /// <summary>
    /// Executes the child steps in parallel using the specified <see cref="ParallelOptions"/>.
    /// </summary>
    /// <param name="context">The step execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task ExecuteStepsAsync(BuilderContext context)
    {
        return System.Threading.Tasks.Parallel.ForEachAsync(Steps, ParallelOptions, ExecuteAsync);

        async ValueTask ExecuteAsync(IStep step, CancellationToken cancellationToken)
        {
            await context.ExecuteAsync(step, cancellationToken);
            context.IncrementProgress(this);
        }
    }

    /// <summary>
    /// Gets the <see cref="ParallelOptions"/> used to control the parallel execution.
    /// </summary>
    protected abstract ParallelOptions ParallelOptions { get; }
}
