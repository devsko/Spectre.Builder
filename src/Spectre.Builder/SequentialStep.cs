// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step that executes a sequence of child steps in order.
/// </summary>
/// <param name="steps">The steps to execute sequentially.</param>
/// <param name="progresses">Optional progress information for the step.</param>
public abstract class SequentialStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses = null) : CompoundStep(steps, progresses), IStep
{
    /// <inheritdoc/>
    protected override async Task ExecuteStepsAsync(BuilderContext context, CancellationToken cancellationToken)
    {
        foreach (IStep step in Steps)
        {
            await context.ExecuteAsync(step);
            context.IncrementProgress(this);
        }
    }
}
