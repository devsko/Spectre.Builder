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
    private readonly List<(IHasProgress, int)> _progresses = [];
    private readonly Dictionary<int, (IHasProgress, int)> _progressById = [];
    private readonly Dictionary<IHasProgress, ProgressTask> _consoleTasks = [];
    private readonly List<(IStep<TContext>, string)> _errors = [];
    private readonly Dictionary<string, IResource> _resources = [];
    private readonly CancellationToken _cancellationToken;
    private int _currentLevel;

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
    int IBuilderContext<TContext>.CurrentLevel
    {
        get => _currentLevel;
        set => _currentLevel = value;
    }

    /// <inheritdoc/>
    public void AddStep(IStep<TContext> step)
    {
        _progresses.Add((step, _currentLevel));
    }

    /// <inheritdoc/>
    public void AddProgress(ProgressInfo progress)
    {
        _progresses.Add((progress, _currentLevel));
    }

    /// <inheritdoc/>
    public (IHasProgress, int) GetProgressAndLevel(int id)
    {
        return _progressById[id];
    }

    /// <summary>
    /// Adds a resource to the context with the specified key.
    /// </summary>
    /// <param name="key">The key for the resource.</param>
    /// <param name="resource">The resource to add.</param>
    /// <returns>The added resource.</returns>
    public T AddResource<T>(string key, T resource) where T : IResource
    {
        _resources.Add(key, resource);
        return resource;
    }

    /// <summary>
    /// Gets a file resource by key.
    /// </summary>
    /// <param name="key">The key of the file resource.</param>
    /// <returns>The <see cref="FileResource"/> associated with the key.</returns>
    public FileResource GetFileResource(string key) => (FileResource)_resources[key];

    /// <summary>
    /// Gets a directory resource by key.
    /// </summary>
    /// <param name="key">The key of the directory resource.</param>
    /// <returns>The <see cref="DirectoryResource"/> associated with the key.</returns>
    public DirectoryResource GetDirectoryResource(string key) => (DirectoryResource)_resources[key];

    /// <inheritdoc/>
    public void SetTotal(IHasProgress progress, long total)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.MaxValue = total;
        }
    }

    /// <inheritdoc/>
    public void SetProgress(IHasProgress progress, long value)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.Value = value;
        }
    }

    /// <inheritdoc/>
    public void IncrementProgress(IHasProgress progress, long amount = 1)
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
    public async Task RunAsync(IStep<TContext> step, StatusInfo[] status)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(status);

        TContext context = Unsafe.As<TContext>(this);

        step.Prepare(context);

        AddProgress(new EmptyInfo { Parent = step });
        foreach (StatusInfo statusInfo in status)
        {
            statusInfo.Parent = step;
            AddProgress(statusInfo);
        }

        await AnsiConsole
            .Progress()
            .Columns([
                new NameColumn(context),
                new ValueColumn(context),
                new NumericalProgress(context),
                new ElapsedColumn(context)])
            .StartAsync(async ctx =>
            {
                foreach ((IHasProgress progress, int level) in _progresses)
                {
                    if (progress.ShouldShowProgress)
                    {
                        ProgressTask task = ctx.AddTask(progress.Name, autoStart: false, maxValue: double.PositiveInfinity);
                        _progressById.Add(task.Id, (progress, level));
                        _consoleTasks.Add(progress, task);
                    }
                }

                Task setStatus = Task.Run(async () =>
                {
                    while (step.State is ProgressState.Running or ProgressState.Wait)
                    {
                        foreach (StatusInfo status in status)
                        {
                            SetProgress(status, status.GetValue());
                        }
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });

                await ExecuteAsync(step);
                await setStatus;

                ctx.Refresh();
            });
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(IStep<TContext> step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
        {
            task.StartTask();
        }

        await step.ExecuteAsync(Unsafe.As<TContext>(this), _cancellationToken);

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
