// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step that contains multiple sub-steps and progress information.
/// </summary>
public abstract class CompoundStep(IEnumerable<IStep> steps, IEnumerable<ProgressInfo>? progresses) : Step, IStep
{
    /// <summary>
    /// Gets the list of sub-steps contained in this compound step.
    /// </summary>
    protected List<IStep> Steps { get; } = [.. steps];

    /// <summary>
    /// Gets the list of progress information items associated with this compound step.
    /// </summary>
    protected List<ProgressInfo> Progresses { get; } = [.. progresses ?? []];

    /// <summary>
    /// Gets the type of progress for this step.
    /// </summary>
    public virtual ProgressType Type => ProgressType.NumericStep;

    /// <inheritdoc/>
    async Task IStep.PrepareAsync(BuilderContext context)
    {
        context.AddStep(this);

        context.Level++;
        foreach (IStep step in Steps)
        {
            await step.PrepareAsync(context);
        }

        foreach (ProgressInfo progress in Progresses)
        {
            progress.Parent = this;
            context.AddProgress(progress);
        }

        context.Level--;
    }

    /// <inheritdoc/>
    async Task IStep.ExecuteAsync(BuilderContext context)
    {
        State = ProgressState.Running;
        context.SetTotal(this, Steps.Count);

        await ExecuteStepsAsync(context);

        State = Steps.All(step => step.State == ProgressState.Skip) ? ProgressState.Skip : ProgressState.Done;
        //context.SetComplete(this);
    }

    /// <summary>
    /// Executes the sub-steps asynchronously.
    /// </summary>
    /// <param name="context">The step context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteStepsAsync(BuilderContext context);
}
