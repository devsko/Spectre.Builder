// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step that performs a conversion operation with progress tracking.
/// </summary>
public abstract class ConversionStep<TContext>(IResource[] inputs, IResource[] outputs) : Step<TContext>, IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    private List<ProgressInfo<TContext>>? _progressInfos;

    /// <summary>
    /// Gets the progress information list for this step.
    /// </summary>
    private List<ProgressInfo<TContext>> ProgressInfos => _progressInfos ??= [];

    /// <inheritdoc/>
    public bool IsHidden { get; set; }

    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => ((IHasProgress<TContext>?)_progressInfos?.LastOrDefault())?.SelfOrLastChild ?? this;

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
        context.Add(this, insertAfter, level);
        OnPrepared(context);

        return ((IHasProgress<TContext>)this).SelfOrLastChild;
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
    /// Adds a progress information object to the step.
    /// </summary>
    /// <param name="progress">The progress information to add.</param>
    /// <param name="context">The context in which the progress is being added.</param>
    protected void Add(ProgressInfo<TContext> progress, TContext context)
    {
        lock (ProgressInfos)
            ProgressInfos.Add(progress);

        context.Add(progress, ((IHasProgress<TContext>)this).SelfOrLastChild, context.GetLevel(this) + 1);
    }

    /// <summary>
    /// Called when the step is prepared.
    /// </summary>
    /// <param name="context">The context in which the step is being prepared.</param>
    protected virtual void OnPrepared(TContext context)
    { }

    /// <summary>
    /// Executes the conversion step asynchronously.
    /// </summary>
    /// <param name="builderContext">The step context.</param>
    /// <param name="timestamp">The timestamp when execution started.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteAsync(TContext builderContext, DateTime timestamp, CancellationToken cancellationToken);
}
