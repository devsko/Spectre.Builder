// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step that executes a sequence of child steps in order.
/// </summary>
/// <param name="steps">The steps to execute sequentially.</param>
/// <param name="createStepsAsync">An optional function to create steps asynchronously.</param>
public abstract class SequentialStep<TContext>(IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync) : CompoundStep<TContext>(steps, createStepsAsync) where TContext : class, IBuilderContext<TContext>
{
    /// <inheritdoc/>
    protected override async Task ExecuteStepsAsync(TContext context, CancellationToken cancellationToken)
    {
        await foreach (Step<TContext> step in StepsToExecute.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await ExecuteStepAsync(step, context, cancellationToken).ConfigureAwait(false);
        }
    }
}
