// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Spectre.Console;

namespace Spectre.Builder;

/// <summary>
/// Provides context and progress management for executing steps with status reporting.
/// </summary>
/// <typeparam name="TContext">
/// The type of the builder context, which must implement <see cref="BuilderContext{TContext}"/>.
/// </typeparam>
public partial class BuilderContext<TContext> where TContext : BuilderContext<TContext>
{
    private readonly object _lock = new();
    private readonly Dictionary<int, (IHasProgress<TContext>, int)> _progressById = [];
    private readonly Dictionary<IHasProgress<TContext>, ProgressTask> _consoleTasks = [];
    private readonly List<(Step<TContext>, string)> _errors = [];
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

    /// <summary>
    /// Adds a progress item to the builder context at the specified nesting level.
    /// </summary>
    /// <param name="progress">The progress item to add.</param>
    /// <param name="insertAfter">
    /// The progress item after which the new item should be inserted. 
    /// If null, the new item is added at the end.
    /// </param>
    /// <param name="level">The nesting level of the progress item.</param>
    /// <returns>The added progress item.</returns>
    public IHasProgress<TContext> Add(IHasProgress<TContext> progress, IHasProgress<TContext>? insertAfter, int level)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (_spectreContext is null)
        {
            throw new InvalidOperationException("Cannot add progress before running the context.");
        }

        if (progress is not Step<TContext> { IsHidden: true })
        {
            ProgressTask task = insertAfter is null
                ? _spectreContext.AddTask("not used", autoStart: false, maxValue: double.PositiveInfinity)
                : _spectreContext.AddTaskAfter("not used", _consoleTasks[insertAfter], autoStart: false, maxValue: double.PositiveInfinity);
            lock (_lock)
            {
                _progressById.Add(task.Id, (progress, level));
                _consoleTasks.Add(progress, task);
            }
        }

        return progress;
    }

    /// <summary>
    /// Retrieves the nesting level of the specified progress item.
    /// </summary>
    /// <param name="progress">The progress item whose level is to be retrieved.</param>
    /// <returns>The nesting level of the specified progress item.</returns>
    public int GetLevel(IHasProgress<TContext> progress)
    {
        return _progressById[(GetTask(progress) ?? throw new KeyNotFoundException()).Id].Item2;
    }

    /// <summary>
    /// Retrieves the progress information and its associated nesting level for a given identifier.
    /// </summary>
    /// <param name="id">The identifier of the progress item.</param>
    /// <returns>
    /// A tuple containing the <see cref="IHasProgress{TContext}"/> instance and its nesting level.
    /// </returns>
    public (IHasProgress<TContext>, int) GetProgressAndLevel(int id)
    {
        lock (_lock)
        {
            return _progressById[id];
        }
    }

    /// <summary>
    /// Sets the total value for the specified progress item.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="total">The total value to set.</param>
    public void SetTotal(IHasProgress<TContext> progress, long total)
    {
        (GetTask(progress) ?? throw new KeyNotFoundException()).MaxValue = total;
    }

    /// <summary>
    /// Sets the current progress value for the specified progress item.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="value">The progress value to set.</param>
    public void SetProgress(IHasProgress<TContext> progress, long value)
    {
        (GetTask(progress) ?? throw new KeyNotFoundException()).Value = value;
    }

    /// <summary>
    /// Increments the progress value for the specified progress item by the given amount.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="amount">The amount to increment the progress by. Defaults to 1.</param>
    public void IncrementProgress(IHasProgress<TContext> progress, long amount = 1)
    {
        (GetTask(progress) ?? throw new KeyNotFoundException()).Increment(amount);
    }

    /// <summary>
    /// Runs the specified step asynchronously with the provided status information and cancellation token.
    /// Prepares the step, adds progress tracking for the step and its statuses, and displays progress using Spectre.Console.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="status">An array of status information to track during execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(Step<TContext> step, StatusInfo<TContext>[] status)
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

                step.Prepare(context, null, 0);

                Add(new EmptyInfo<TContext>(), null, 0);
                foreach (StatusInfo<TContext> statusInfo in status)
                {
                    Add(statusInfo, null, 0);
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

    /// <summary>
    /// Executes the specified step asynchronously within the context.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the step.
    /// </returns>
    public async Task ExecuteAsync(Step<TContext> step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);

        GetTask(step)?.StartTask();

        await step.ExecuteAsync(Unsafe.As<TContext>(this), cancellationToken).ConfigureAwait(false);

        GetTask(step)?.StopTask();

        // Failed?
    }

    /// <summary>
    /// Marks the specified step as failed and records the associated error message.
    /// </summary>
    /// <param name="step">The step that failed.</param>
    /// <param name="error">The error message describing the failure.</param>
    public void Fail(Step<TContext> step, string error)
    {
        lock (_lock)
        {
            _errors.Add((step, error));
        }
    }

    /// <summary>
    /// Ensures that the context is in a valid state for further execution.
    /// Throws an exception if the context is invalid.
    /// </summary>
    public void EnsureValid()
    {
        if (_errors.Count > 0)
        {
            lock (_lock)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, _errors.Select(e => e.Item2)));
            }
        }
    }

    private ProgressTask? GetTask(IHasProgress<TContext> progress)
    {
        lock (_lock)
        {
            return _consoleTasks.TryGetValue(progress, out ProgressTask? task) ? task : null;
        }
    }
}
