// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Spectre.Builder;

public partial class StepContext
{
    private abstract class StepProgressColumn(StepContext context) : ProgressColumn
    {
        protected (IHasProgress Step, int Level) GetStepAndLevel(ProgressTask task)
        {
            return context._progresses[int.Parse(task.Description)];
        }

        public sealed override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            (IHasProgress step, int level) = GetStepAndLevel(task);
            return Render(options, task, step, level, deltaTime);
        }

        protected abstract IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress step, int level, TimeSpan deltaTime);
    }

    private sealed class NameColumn(StepContext context) : StepProgressColumn(context)
    {
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress step, int level, TimeSpan deltaTime)
        {
            return new Markup($"[conceal][/]{new string(' ', level * 2)}{step.Name}").Overflow(Overflow.Ellipsis);
        }
    }

    private sealed class NumericalProgress(StepContext context) : StepProgressColumn(context)
    {
        private static readonly Markup _empty = new("");

        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress step, int level, TimeSpan deltaTime)
        {
            return ((step.State, step.Type & ProgressType.NumericMask) switch
            {
                (_, 0) => _empty,
                (ProgressState.Wait, _) => new Markup("Wait", Color.Grey),
                (ProgressState.Done, _) => new Markup("Done", Color.Green),
                (ProgressState.Skip, _) => new Markup("Skip", Color.Yellow),
                (ProgressState.Running, ProgressType.NumericPercentage) => double.IsPositiveInfinity(task.MaxValue) ? _empty : new Markup($"{(int)task.Percentage}%"),
                (ProgressState.Running, ProgressType.NumericStep) => new Markup($"{(int)task.Value}[grey]/[/]{(int)task.MaxValue}"),
                _ => _empty,
            }).RightJustified();
        }

        public override int? GetColumnWidth(RenderOptions options)
        {
            return 5;
        }
    }

    private sealed class ElapsedColumn(StepContext context) : StepProgressColumn(context)
    {
        protected override bool NoWrap => true;

        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress step, int level, TimeSpan deltaTime)
        {
            return (step.State is ProgressState.Running or ProgressState.Done) &&
                (step.Type & ProgressType.ElapsedMask) is ProgressType.ElapsedVisible &&
                task.ElapsedTime is not null
                ? task.ElapsedTime.Value.TotalHours > 99 ? new Markup("**:**:**") : new Text($"{task.ElapsedTime.Value:hh\\:mm\\:ss}", Color.Grey)
                : Text.Empty;
        }

        public override int? GetColumnWidth(RenderOptions options)
        {
            return 8;
        }
    }

    private sealed class ValueColumn(StepContext context) : StepProgressColumn(context)
    {
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress step, int level, TimeSpan deltaTime)
        {
            if ((step.State is ProgressState.Running or ProgressState.Done || step is StatusInfo))
            {
                Markup result;
                switch (step.Type & ProgressType.ValueMask)
                {
                    case ProgressType.ValueDataSize:
                    {
                        DataSize? total = double.IsPositiveInfinity(task.MaxValue) ? null : new((int)task.MaxValue);
                        DataSize value = total is null ? new((int)task.Value) : new((int)task.Value, total.Value.Unit);
                        string suffix = total is null ? value.Suffix : total.Value.Suffix;

                        result = step.State is ProgressState.Done or ProgressState.Skip
                            ? new Markup($"{value.Format()} [grey]{suffix}[/]")
                            : new Markup($"{(total is null ? value.Format() : $"{value.Format()}[grey]/[/]{total.Value.Format()}")} [grey]{suffix}[/]");
                    }

                    return result.RightJustified();
                    case ProgressType.ValueRaw:
                    {
                        if (step.State is ProgressState.Done or ProgressState.Skip)
                        {
                            result = new Markup($"{(long)task.Value:N0}");
                        }
                        else
                        {
                            long? total = double.IsPositiveInfinity(task.MaxValue) ? null : (long)task.MaxValue;
                            result = new Markup(total is null ? $"{(long)task.Value:N0}" : $"{(long)task.Value:N0}[grey]/[/]{total.Value:N0}");
                        }
                    }

                    return result.RightJustified();
                    case ProgressType.ValueTimeSpan:
                    {
                        result = new Markup(new TimeSpan((long)task.Value).ToString("hh\\:mm\\:ss"));
                    }

                    return result.RightJustified();
                }
            }

            return Text.Empty;
        }

        public override int? GetColumnWidth(RenderOptions options)
        {
            return 20;
        }
    }
}
