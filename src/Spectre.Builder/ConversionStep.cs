// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step that performs a conversion operation with progress tracking.
/// </summary>
public abstract class ConversionStep<TContext> : Step<TContext>, IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    private IResource[]? _inputs;
    private IResource[]? _outputs;

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

    /// <summary>
    /// Gets a value indicating whether to hide the step when skipped.
    /// </summary>
    protected virtual bool HideWhenSkipped => false;

    /// <inheritdoc/>
    public override bool ShouldShowProgress
    {
        get
        {
            Debug.Assert(_inputs is not null);
            Debug.Assert(_outputs is not null);

            return !HideWhenSkipped || _outputs.Any(output => !output.IsAvailable) || _inputs.Length != 0;
            // TODO Should there be a MaxAge?
        }
    }

    /// <inheritdoc/>
    void IStep<TContext>.Prepare(TContext context)
    {
        _inputs = [.. GetInputs(context)];
        _outputs = [.. GetOutputs(context)];

        context.AddStep(this);

        context.CurrentLevel++;
        foreach (ProgressInfo progress in GetProgressInfos())
        {
            progress.Parent = this;
            context.AddProgress(progress);
        }
        context.CurrentLevel--;
    }

    /// <inheritdoc/>
    async Task IStep<TContext>.ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        Debug.Assert(_inputs is not null);
        Debug.Assert(_outputs is not null);

        foreach (IResource missing in _inputs.Where(input => !input.IsAvailable))
        {
            context.Fail(this, $"Input {missing.Name} not available.");
        }

        context.EnsureValid();

        StepExecution execution;

        if (_outputs.Any(output => !output.IsAvailable))
        {
            execution = StepExecution.Necessary;
        }
        else
        {
            DateTimeOffset newestInput = _inputs.Length != 0 ? _inputs.Min(input => input.LastUpdated ?? DateTimeOffset.MaxValue) : DateTimeOffset.MinValue;
            DateTimeOffset oldesOutput = _outputs.Length != 0 ? _outputs.Max(output => output.LastUpdated ?? DateTimeOffset.MinValue) : DateTimeOffset.MinValue;

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
            await ExecuteAsync(context, DateTime.UtcNow, cancellationToken);
            State = ProgressState.Done;
        }

        //context.SetComplete(this);

        foreach (IResource missing in _outputs.Where(output => !output.IsAvailable))
        {
            context.Fail(this, $"Output {missing.Name} not created.");
        }

        context.EnsureValid();
    }

    /// <summary>
    /// Gets the input resources for this step.
    /// </summary>
    /// <returns>An enumerable of input resources.</returns>
    protected virtual IEnumerable<IResource> GetInputs(TContext context) => [];

    /// <summary>
    /// Gets the output resources for this step.
    /// </summary>
    /// <returns>An enumerable of output resources.</returns>
    protected virtual IEnumerable<IResource> GetOutputs(TContext context) => [];

    /// <summary>
    /// Gets the progress information items for this step.
    /// </summary>
    /// <returns>An enumerable of progress information items.</returns>
    protected virtual IEnumerable<ProgressInfo> GetProgressInfos() => [];

    /// <summary>
    /// Executes the conversion step asynchronously.
    /// </summary>
    /// <param name="builderContext">The step context.</param>
    /// <param name="timestamp">The timestamp when execution started.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteAsync(TContext builderContext, DateTime timestamp, CancellationToken cancellationToken);
}
