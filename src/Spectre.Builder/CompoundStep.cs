// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

public abstract class CompoundStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses) : Step, IStep
{
    protected List<IStep> Steps { get; } = [.. steps];

    protected List<ProgressInfo> Progresses { get; } = [.. progresses ?? []];

    public virtual ProgressType Type => ProgressType.NumericStep;

    void IStep.Prepare(StepContext context)
    {
        context.AddStep(this);

        context.Level++;
        foreach (IStep step in Steps)
        {
            step.Prepare(context);
        }

        foreach (ProgressInfo progress in Progresses)
        {
            progress.Parent = this;
            context.AddProgress(progress);
        }

        context.Level--;
    }

    async Task IStep.ExecuteAsync(StepContext context)
    {
        State = ProgressState.Running;
        context.SetTotal(this, Steps.Count);

        await ExecuteStepsAsync(context);

        State = Steps.All(step => step.State == ProgressState.Skip) ? ProgressState.Skip : ProgressState.Done;
        //context.SetComplete(this);
    }

    protected abstract Task ExecuteStepsAsync(StepContext context);
}
