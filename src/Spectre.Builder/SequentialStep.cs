// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public abstract class SequentialStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses = null) : CompoundStep(steps, progresses), IStep
{
    protected override async Task ExecuteStepsAsync(StepContext context)
    {
        foreach (IStep step in Steps)
        {
            await context.ExecuteAsync(step, default);
            context.IncrementProgress(this);
        }
    }
}
