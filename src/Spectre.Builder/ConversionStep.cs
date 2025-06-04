// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Spectre.Builder;

public abstract class ConversionStep : Step, IStep
{
    private IResource[]? _inputs;
    private IResource[]? _outputs;

    public virtual ProgressType Type => (ShowProgressValue ? ShowFileSizeProgress ? ProgressType.ValueDataSize : ProgressType.ValueRaw : 0) | ProgressType.NumericPercentage | ProgressType.ElapsedVisible;

    protected virtual bool ShowProgressValue => true;

    protected virtual bool ShowFileSizeProgress => true;

    protected virtual bool HideWhenSkipped => false;

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

    void IStep.Prepare(StepContext context)
    {
        _inputs = [.. GetInputs()];
        _outputs = [.. GetOutputs()];

        context.AddStep(this);

        context.Level++;
        foreach (ProgressInfo progress in GetProgressInfos())
        {
            progress.Parent = this;
            context.AddProgress(progress);
        }
        context.Level--;
    }

    async Task IStep.ExecuteAsync(StepContext context)
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
            await ExecuteAsync(context, DateTime.UtcNow);
            State = ProgressState.Done;
        }

        //context.SetComplete(this);

        foreach (IResource missing in _outputs.Where(output => !output.IsAvailable))
        {
            context.Fail(this, $"Output {missing.Name} not created.");
        }

        context.EnsureValid();
    }

    protected virtual IEnumerable<IResource> GetInputs() => [];
    protected virtual IEnumerable<IResource> GetOutputs() => [];
    protected virtual IEnumerable<ProgressInfo> GetProgressInfos() => [];
    protected abstract Task ExecuteAsync(StepContext context, DateTime timestamp);
}
