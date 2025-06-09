// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step that contains multiple sub-steps and progress information.
/// </summary>
public abstract class CompoundStep<TContext>(IEnumerable<IStep<TContext>> steps, IEnumerable<ProgressInfo>? progresses) : Step<TContext>, IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// Gets the list of sub-steps contained in this compound step.
    /// </summary>
    protected List<IStep<TContext>> Steps { get; } = [.. steps];

    /// <summary>
    /// Gets the list of progress information items associated with this compound step.
    /// </summary>
    protected List<ProgressInfo> Progresses { get; } = [.. progresses ?? []];

    /// <summary>
    /// Gets the type of progress for this step.
    /// </summary>
    public virtual ProgressType Type => ProgressType.NumericStep;

    /// <inheritdoc/>
    void IStep<TContext>.Prepare(TContext context)
    {
        context.AddStep(this);

        context.CurrentLevel++;
        foreach (IStep<TContext> step in Steps)
        {
            step.Prepare(context);
        }
        foreach (ProgressInfo progress in Progresses)
        {
            progress.Parent = this;
            context.AddProgress(progress);
        }
        context.CurrentLevel--;
    }

    /// <inheritdoc/>
    async Task IStep<TContext>.ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        State = ProgressState.Running;
        context.SetTotal(this, Steps.Count);

        await ExecuteStepsAsync(context, cancellationToken);

        State = Steps.All(step => step.State == ProgressState.Skip) ? ProgressState.Skip : ProgressState.Done;
        //context.SetComplete(this);
    }

    /// <summary>
    /// Executes the sub-steps asynchronously.
    /// </summary>
    /// <param name="context">The step context.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteStepsAsync(TContext context, CancellationToken cancellationToken);
}
