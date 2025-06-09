// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Spectre.Console;

namespace Spectre.Builder;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface IBuilderContext<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// 
    /// </summary>
    Dictionary<int, (IHasProgress, int)> Progresses { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="step"></param>
    void AddStep(IStep<TContext> step);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progress"></param>
    void AddProgress(ProgressInfo progress);

    /// <summary>
    /// 
    /// </summary>
    int Level { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="total"></param>
    void SetTotal(IHasProgress progress, long total);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="amount"></param>
    void IncrementProgress(IHasProgress progress, long amount = 1);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    ValueTask ExecuteAsync(IStep<TContext> step);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="step"></param>
    /// <param name="error"></param>
    void Fail(IStep<TContext> step, string error);

    /// <summary>
    /// 
    /// </summary>
    void EnsureValid();
}

/// <summary>
/// Provides context and progress management for executing steps with status reporting.
/// </summary>
public partial class BuilderContext<TContext>(CancellationToken cancellationToken) : IBuilderContext<TContext> where TContext : class, IBuilderContext<TContext>
{
    private readonly Dictionary<int, (IHasProgress, int)> _progresses = [];
    private readonly Dictionary<IHasProgress, ProgressTask> _consoleTasks = [];
    private readonly List<(IStep<TContext>, string)> _errors = [];
    private readonly Dictionary<string, IResource> _resources = [];
    private readonly CancellationToken _cancellationToken = cancellationToken;

    Dictionary<int, (IHasProgress, int)> IBuilderContext<TContext>.Progresses => _progresses;

    /// <summary>
    /// Gets or sets the current step level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Adds a step to the context for progress tracking.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(IStep<TContext> step)
    {
        _progresses.Add(step.GetHashCode(), (step, Level));
    }

    /// <summary>
    /// Adds a progress information item to the context.
    /// </summary>
    /// <param name="progress">The progress information to add.</param>
    public void AddProgress(ProgressInfo progress)
    {
        _progresses.Add(progress.GetHashCode(), (progress, Level));
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

    /// <summary>
    /// Sets the total value for the specified progress step.
    /// </summary>
    /// <param name="progress">The progress step.</param>
    /// <param name="total">The total value to set.</param>
    public void SetTotal(IHasProgress progress, long total)
    {
        if (_consoleTasks.TryGetValue(progress, out ProgressTask? task))
        {
            task.MaxValue = total;
        }
    }

    /// <summary>
    /// Sets the progress value for the specified step.
    /// </summary>
    /// <param name="step">The progress step.</param>
    /// <param name="progress">The progress value to set.</param>
    public void SetProgress(IHasProgress step, long progress)
    {
        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
        {
            task.Value = progress;
        }
    }

    /// <summary>
    /// Increments the progress value for the specified step by the given amount.
    /// </summary>
    /// <param name="progress">The progress step.</param>
    /// <param name="amount">The amount to increment by. Defaults to 1.</param>
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
                foreach ((IHasProgress progress, int level) in _progresses.Values)
                {
                    if (progress.ShouldShowProgress)
                    {
                        _consoleTasks.Add(progress, ctx.AddTask(progress.GetHashCode().ToString(), autoStart: false, maxValue: double.PositiveInfinity));
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

    /// <summary>
    /// Executes the specified step asynchronously within this context.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask ExecuteAsync(IStep<TContext> step)
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

    /// <summary>
    /// Marks the specified step as failed with the given error message.
    /// </summary>
    /// <param name="step">The step that failed.</param>
    /// <param name="error">The error message.</param>
    public void Fail(IStep<TContext> step, string error)
    {
        _errors.Add((step, error));
    }

    /// <summary>
    /// Ensures that the context is valid and throws an exception if any errors have occurred.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if any errors have been recorded.</exception>
    public void EnsureValid()
    {
        if (_errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, _errors.Select(e => e.Item2)));
        }
    }
}
