// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step that performs a conversion operation with progress tracking.
/// </summary>
public abstract class ConversionStep<TContext>(IResource[] inputs, IResource[] outputs, ProgressInfo<TContext>[]? progressInfos = null) : Step<TContext>, IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => ((IHasProgress<TContext>?)progressInfos?.LastOrDefault())?.SelfOrLastChild ?? this;

    /// <summary>
    /// Gets the type of progress information to display for this step.
    /// </summary>
    public virtual ProgressType Type => (ShowProgressValue ? ShowProgressAsDataSize ? ProgressType.ValueDataSize : ProgressType.ValueRaw : 0) | ProgressType.NumericPercentage | ProgressType.ElapsedVisible;

    /// <summary>
    /// Gets a value indicating whether to show the progress value.
    /// </summary>
    protected virtual bool ShowProgressValue => true;

    /// <summary>
    /// Gets a value indicating whether to show file size progress.
    /// </summary>
    protected virtual bool ShowProgressAsDataSize => true;

    /// <inheritdoc/>
    IHasProgress<TContext> IStep<TContext>.Prepare(TContext context, IHasProgress<TContext>? insertAfter, int level)
    {
        insertAfter = context.Add(this, insertAfter, level);

        foreach (ProgressInfo<TContext> progress in progressInfos ?? [])
        {
            progress.Parent = this;
            insertAfter = context.Add(progress, insertAfter, level + 1);
        }

        return insertAfter;
    }

    /// <inheritdoc/>
    async Task IStep<TContext>.ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (IResource resource in inputs.Concat(outputs))
        {
            await resource.DetermineAvailabilityAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (IResource missing in inputs.Where(input => input.IsRequired && !input.IsAvailable))
        {
            context.Fail(this, $"Required input {missing.Name} not available.");
        }

        context.EnsureValid();

        StepExecution execution;

        if (outputs.Any(output => !output.IsAvailable))
        {
            execution = StepExecution.Necessary;
        }
        else if (inputs.Any(input => !input.IsAvailable))
        {
            execution = StepExecution.Redundant;
        }
        else
        {
            DateTimeOffset newestInput = inputs.Length != 0 ? inputs.Min(input => input.LastUpdated ?? DateTimeOffset.MaxValue) : DateTimeOffset.MinValue;
            DateTimeOffset oldesOutput = outputs.Length != 0 ? outputs.Max(output => output.LastUpdated ?? DateTimeOffset.MinValue) : DateTimeOffset.MinValue;

            execution = oldesOutput <= newestInput ? StepExecution.Recommended : StepExecution.Redundant;
        }

        // Ask context

        if (execution == StepExecution.Redundant)
        {
            State = ProgressState.Skip;
        }
        else
        {
            State = ProgressState.Running;
            await ExecuteAsync(context, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
            State = ProgressState.Done;
        }

        //context.SetComplete(this);

        foreach (IResource resource in outputs)
        {
            await resource.DetermineAvailabilityAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (IResource missing in outputs.Where(output => !output.IsAvailable))
        {
            context.Fail(this, $"Output {missing.Name} not created.");
        }

        context.EnsureValid();
    }

    /// <summary>
    /// Executes the conversion step asynchronously.
    /// </summary>
    /// <param name="builderContext">The step context.</param>
    /// <param name="timestamp">The timestamp when execution started.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteAsync(TContext builderContext, DateTime timestamp, CancellationToken cancellationToken);
}
