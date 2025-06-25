// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step that executes a sequence of child steps in order.
/// </summary>
/// <param name="steps">The steps to execute sequentially.</param>
/// <param name="progresses">Optional progress information for the step.</param>
public abstract class SequentialStep<TContext>(IEnumerable<IStep<TContext>> steps, IEnumerable<ProgressInfo<TContext>>? progresses = null) : CompoundStep<TContext>(steps, progresses), IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <inheritdoc/>
    protected override async Task ExecuteStepsAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (IStep<TContext> step in Steps)
        {
            await context.ExecuteAsync(step, cancellationToken);
            context.IncrementProgress(this);
        }
    }
}
