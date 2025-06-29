// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Spectre.Console;

namespace Spectre.Builder;

/// <summary>
/// Provides context and progress management for executing steps with status reporting.
/// </summary>
public partial class BuilderContext<TContext> : IBuilderContext<TContext> where TContext : class, IBuilderContext<TContext>
{
    private readonly List<(IHasProgress<TContext>, int)> _progresses = [];
    private readonly Dictionary<int, (IHasProgress<TContext>, int)> _progressById = [];
    private readonly Dictionary<IHasProgress<TContext>, ProgressTask> _consoleTasks = [];
    private readonly List<(IStep<TContext>, string)> _errors = [];
    private readonly CancellationToken _cancellationToken;
    private ProgressContext? _spectreContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuilderContext{TContext}"/> class
    /// with the specified cancellation token.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the current instance does not match the generic type parameter <typeparamref name="TContext"/>.
    /// </exception>
    public BuilderContext(CancellationToken cancellationToken)
    {
        if (this is not TContext)
        {
            throw new InvalidOperationException();
        }

        _cancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    public IHasProgress<TContext> Add(IHasProgress<TContext> progress, IHasProgress<TContext>? insertAfter, int level)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (_spectreContext is null)
        {
            throw new InvalidOperationException("Cannot add progress before running the context.");
        }

        _progresses.Add((progress, level));
        ProgressTask task = insertAfter is null
            ? _spectreContext.AddTask("not used", autoStart: false, maxValue: double.PositiveInfinity)
            : _spectreContext.AddTaskAfter("not used", _consoleTasks[insertAfter], autoStart: false, maxValue: double.PositiveInfinity);
        _progressById.Add(task.Id, (progress, level));
        _consoleTasks.Add(progress, task);

        return progress;
    }

    /// <inheritdoc/>
    public int GetLevel(IHasProgress<TContext> progress)
    {
        return _consoleTasks.TryGetValue(progress, out ProgressTask? task)
            ? _progressById[task.Id].Item2
            : throw new KeyNotFoundException("Progress not found in the context.");
    }

    /// <inheritdoc/>
    public (IHasProgress<TContext>, int) GetProgressAndLevel(int id)
    {
        return _progressById[id];
    }

    /// <inheritdoc/>
    public void SetTotal(IHasProgress<TContext> progress, long total)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.MaxValue = total;
        }
    }

    /// <inheritdoc/>
    public void SetProgress(IHasProgress<TContext> progress, long value)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.Value = value;
        }
    }

    /// <inheritdoc/>
    public void IncrementProgress(IHasProgress<TContext> progress, long amount = 1)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.Increment(amount);
        }
    }

    /// <summary>
    /// Runs the specified step asynchronously with the provided status information and cancellation token.
    /// Prepares the step, adds progress tracking for the step and its statuses, and displays progress using Spectre.Console.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="status">An array of status information to track during execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(IStep<TContext> step, StatusInfo<TContext>[] status)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(status);

        TContext context = Unsafe.As<TContext>(this);

        await AnsiConsole
            .Progress()
            .Columns([
                new NameColumn(context),
                new ValueColumn(context),
                new NumericalProgress(context),
                new ElapsedColumn(context)])
            .StartAsync(async ctx =>
            {
                _spectreContext = ctx;

                IHasProgress<TContext> insertAfter = step.Prepare(context, null, 0);

                Add(new EmptyInfo<TContext> { Parent = step }, insertAfter, 0);
                foreach (StatusInfo<TContext> statusInfo in status)
                {
                    statusInfo.Parent = step;
                    insertAfter = Add(statusInfo, insertAfter, 0);
                }

                Task setStatus = Task.Run(async () =>
                {
                    while (step.State is ProgressState.Running or ProgressState.Wait)
                    {
                        foreach (StatusInfo<TContext> status in status)
                        {
                            SetProgress(status, status.GetValue());
                        }
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                });

                await ExecuteAsync(step, _cancellationToken).ConfigureAwait(false);
                await setStatus.ConfigureAwait(false);

                ctx.Refresh();

                _spectreContext = null;
            }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(IStep<TContext> step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
        {
            task.StartTask();
        }

        await step.ExecuteAsync(Unsafe.As<TContext>(this), cancellationToken).ConfigureAwait(false);

        if (_consoleTasks.TryGetValue(step, out task))
        {
            task.StopTask();
        }

        // Failed?
    }

    /// <inheritdoc/>
    public void Fail(IStep<TContext> step, string error)
    {
        _errors.Add((step, error));
    }

    /// <inheritdoc/>
    public void EnsureValid()
    {
        if (_errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, _errors.Select(e => e.Item2)));
        }
    }
}
