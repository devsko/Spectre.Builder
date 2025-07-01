// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Spectre.Builder;

/// <summary>
/// Represents a step that contains multiple sub-steps and progress information.
/// </summary>
public abstract class CompoundStep<TContext>(IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync) : Step<TContext>, IHasProgress<TContext> where TContext : BuilderContext<TContext>
{
    private readonly List<Step<TContext>> _steps = [.. steps.Where(step => !step.IsHidden)];
    private bool _allStepsSkipped = true;

    /// <summary>
    /// Gets the channel used to manage the execution of sub-steps.
    /// </summary>
    protected Channel<Step<TContext>> StepsToExecute { get; } = Channel.CreateUnbounded<Step<TContext>>();

    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => ((IHasProgress<TContext>?)_steps.LastOrDefault())?.SelfOrLastChild ?? this;

    /// <summary>
    /// Gets the type of progress for this step.
    /// </summary>
    public override ProgressType Type => ProgressType.NumericStep;

    /// <summary>
    /// Adds a sub-step to the compound step and updates the context with the total number of steps.
    /// </summary>
    /// <param name="step">The sub-step to add.</param>
    /// <param name="context">The context in which the step is being executed.</param>
    public void Add(Step<TContext> step, TContext context)
    {
        step.Prepare(context, ((IHasProgress<TContext>?)this)?.SelfOrLastChild, context.GetLevel(this) + 1);

        if (!step.IsHidden)
        {
            lock (_steps)
                _steps.Add(step);
        }

        StepsToExecute.Writer.TryWrite(step);

        context.SetTotal(this, _steps.Count);
    }

    /// <inheritdoc/>
    protected internal override IHasProgress<TContext> Prepare(TContext context, IHasProgress<TContext>? insertAfter, int level)
    {
        insertAfter = context.Add(this, insertAfter, level);

        foreach (Step<TContext> step in steps)
        {
            insertAfter = step.Prepare(context, insertAfter, level + 1);
            StepsToExecute.Writer.TryWrite(step);
        }

        return insertAfter;
    }

    /// <inheritdoc/>
    protected internal override async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        State = ProgressState.Running;
        context.SetTotal(this, _steps.Count);

        Task executeSteps = ExecuteStepsAsync(context, cancellationToken);

        if (createStepsAsync is not null)
        {
            await createStepsAsync(this, context, cancellationToken).ConfigureAwait(false);
        }

        StepsToExecute.Writer.Complete();

        await executeSteps.ConfigureAwait(false);

        State = _allStepsSkipped ? ProgressState.Skip : ProgressState.Done;
        //context.SetComplete(this);
    }

    /// <summary>
    /// Executes the sub-steps asynchronously.
    /// </summary>
    /// <param name="context">The step context.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteStepsAsync(TContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a single sub-step asynchronously.
    /// </summary>
    /// <param name="step">The sub-step to execute.</param>
    /// <param name="context">The context in which the step is being executed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async ValueTask ExecuteStepAsync(Step<TContext> step, TContext context, CancellationToken cancellationToken)
    {
        await context.ExecuteAsync(step, cancellationToken).ConfigureAwait(false);
        _allStepsSkipped &= step.IsHidden || step.State is ProgressState.Skip;
        if (!step.IsHidden)
        {
            context.IncrementProgress(this);
        }
    }
}
