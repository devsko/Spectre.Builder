// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a compound step that executes its child steps in parallel.
/// </summary>
public abstract class ParallelStep<TContext>(IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync) : CompoundStep<TContext>(steps, createStepsAsync) where TContext : BuilderContext<TContext>
{
    /// <summary>
    /// Executes the child steps in parallel using the specified <see cref="ParallelOptions"/>.
    /// </summary>
    /// <param name="context">The step execution context.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task ExecuteStepsAsync(TContext context, CancellationToken cancellationToken)
    {
        ParallelOptions.CancellationToken = cancellationToken;
        int threadCount = Math.Min(Environment.ProcessorCount, (ParallelOptions.TaskScheduler ?? TaskScheduler.Current).MaximumConcurrencyLevel);
        return System.Threading.Tasks.Parallel.ForAsync(0, threadCount - 1, ParallelOptions, ExecuteAsync);

        async ValueTask ExecuteAsync(int _, CancellationToken cancellationToken)
        {
            await foreach (Step<TContext> step in StepsToExecute.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await ExecuteStepAsync(step, context, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="ParallelOptions"/> used to control the parallel execution.
    /// </summary>
    protected abstract ParallelOptions ParallelOptions { get; }
}
