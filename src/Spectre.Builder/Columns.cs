// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Spectre.Builder;

public partial class BuilderContext<TContext>
{
    /// <summary>
    /// Represents a base class for custom progress columns associated with a step context.
    /// </summary>
    /// <param name="context">The step context.</param>
    private abstract class StepProgressColumn(TContext context) : ProgressColumn
    {
        protected TContext Context { get; } = context;

        /// <inheritdoc/>
        public sealed override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            (IHasProgress<TContext> step, int level) = Context.GetProgressAndLevel(task.Id);
            return Render(options, task, step, level, deltaTime);
        }

        /// <summary>
        /// Renders the column for the specified step and level.
        /// </summary>
        /// <param name="options">The render options.</param>
        /// <param name="task">The progress task.</param>
        /// <param name="step">The step.</param>
        /// <param name="level">The level of the step.</param>
        /// <param name="deltaTime">The time since the last render.</param>
        /// <returns>The rendered column.</returns>
        protected abstract IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress<TContext> step, int level, TimeSpan deltaTime);
    }

    /// <summary>
    /// Represents a column that displays the name of the step.
    /// </summary>
    /// <param name="context">The step context.</param>
    private sealed class NameColumn(TContext context) : StepProgressColumn(context)
    {
        /// <inheritdoc/>
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress<TContext> step, int level, TimeSpan deltaTime)
        {
            return new Markup($"[conceal][/]{new string(' ', level * 2)}{step.GetName(Context)}").Overflow(Overflow.Ellipsis);
        }
    }

    /// <summary>
    /// Represents a column that displays numerical progress information.
    /// </summary>
    /// <param name="context">The step context.</param>
    private sealed class NumericalProgress(TContext context) : StepProgressColumn(context)
    {
        private static readonly Markup _empty = new("");

        /// <inheritdoc/>
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress<TContext> step, int level, TimeSpan deltaTime)
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

        /// <inheritdoc/>
        public override int? GetColumnWidth(RenderOptions options)
        {
            return 5;
        }
    }

    /// <summary>
    /// Represents a column that displays elapsed time for a step.
    /// </summary>
    /// <param name="context">The step context.</param>
    private sealed class ElapsedColumn(TContext context) : StepProgressColumn(context)
    {
        /// <inheritdoc/>
        protected override bool NoWrap => true;

        /// <inheritdoc/>
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress<TContext> step, int level, TimeSpan deltaTime)
        {
            return (step.State is ProgressState.Running or ProgressState.Done) &&
                (step.Type & ProgressType.ElapsedMask) is ProgressType.ElapsedVisible &&
                task.ElapsedTime is not null
                ? task.ElapsedTime.Value.TotalHours > 99 ? new Markup("**:**:**") : new Text($"{task.ElapsedTime.Value:hh\\:mm\\:ss}", Color.Grey)
                : Text.Empty;
        }

        /// <inheritdoc/>
        public override int? GetColumnWidth(RenderOptions options)
        {
            return 8;
        }
    }

    /// <summary>
    /// Represents a column that displays value information for a step.
    /// </summary>
    /// <param name="context">The step context.</param>
    private sealed class ValueColumn(TContext context) : StepProgressColumn(context)
    {
        /// <inheritdoc/>
        protected override IRenderable Render(RenderOptions options, ProgressTask task, IHasProgress<TContext> step, int level, TimeSpan deltaTime)
        {
            if (step.State is not ProgressState.Skip)
            {
                Markup result;
                switch (step.Type & ProgressType.ValueMask)
                {
                    case ProgressType.ValueDataSize:
                        {
                            DataSize? total = double.IsPositiveInfinity(task.MaxValue) ? null : new((int)task.MaxValue);
                            if (step.State is ProgressState.Wait)
                            {
                                if (total is null)
                                {
                                    break;
                                }
                                result = new Markup($"{total.Value.Format()} [grey]{total.Value.Suffix}[/]");
                            }
                            else
                            {
                                DataSize value = total is null ? new((int)task.Value) : new((int)task.Value, total.Value.Unit);
                                string suffix = total is null ? value.Suffix : total.Value.Suffix;

                                result = step.State is ProgressState.Done
                                    ? new Markup($"{value.Format()} [grey]{suffix}[/]")
                                    : new Markup($"{(total is null ? value.Format() : $"{value.Format()}[grey]/[/]{total.Value.Format()}")} [grey]{suffix}[/]");
                            }
                        }
                        return result.RightJustified();
                    case ProgressType.ValueRaw:
                        {
                            if (step.State is ProgressState.Done)
                            {
                                result = new Markup($"{(long)task.Value:N0}");
                            }
                            else
                            {
                                long? total = double.IsPositiveInfinity(task.MaxValue) ? null : (long)task.MaxValue;
                                if (step.State is ProgressState.Wait)
                                {
                                    if (total is null)
                                    {
                                        break;
                                    }
                                    result = new Markup($"{total.Value:N0}");
                                }
                                else
                                {
                                    result = new Markup(total is null ? $"{(long)task.Value:N0}" : $"{(long)task.Value:N0}[grey]/[/]{total.Value:N0}");
                                }
                            }
                        }
                        return result.RightJustified();
                    case ProgressType.ValueTimeSpan:
                        if (step.State is ProgressState.Wait)
                        {
                            break;
                        }

                        return new Markup(new TimeSpan((long)task.Value).ToString("hh\\:mm\\:ss")).RightJustified();
                }
            }

            return Text.Empty;
        }

        /// <inheritdoc/>
        public override int? GetColumnWidth(RenderOptions options)
        {
            return 20;
        }
    }
}
