// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Spectre.Builder;

/// <summary>
/// Provides context and progress management for executing steps with status reporting.
/// </summary>
public partial class BuilderContext
{
    /// <summary>
    /// Runs the specified step asynchronously with the given status information.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="status">An array of status information to track.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync(IStep step, StatusInfo[] status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(status);

        BuilderContext context = new();
        await step.PrepareAsync(context);

        context.AddProgress(new EmptyInfo { Parent = step });
        foreach (StatusInfo statusInfo in status)
        {
            statusInfo.Parent = step;
            context.AddProgress(statusInfo);
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
                foreach ((IHasProgress progress, int level) in context._progresses.Values)
                {
                    if (progress.ShouldShowProgress)
                    {
                        context._consoleTasks.Add(progress, ctx.AddTask(progress.GetHashCode().ToString(), autoStart: false, maxValue: double.PositiveInfinity));
                    }
                }

                Task setStatus = Task.Run(async () =>
                {
                    while (step.State is ProgressState.Running or ProgressState.Wait)
                    {
                        foreach (StatusInfo status in status)
                        {
                            context.SetProgress(status, status.GetValue());
                        }
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });

                await context.ExecuteAsync(step, cancellationToken);
                await setStatus;

                ctx.Refresh();
            });
    }

    private readonly Dictionary<int, (IHasProgress, int)> _progresses = [];
    private readonly Dictionary<IHasProgress, ProgressTask> _consoleTasks = [];
    private readonly List<(IStep, string)> _errors = [];
    private readonly Dictionary<string, IResource> _resources = [];

    /// <summary>
    /// Gets or sets the current step level.
    /// </summary>
    public int Level { get; set; }

    private BuilderContext()
    { }

    /// <summary>
    /// Adds a step to the context for progress tracking.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(IStep step)
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
    public T AddResource<T>(T resource, string key) where T : IResource
    {
        _resources.Add(key, resource);
        return resource;
    }

    /// <summary>
    /// Adds a resource to the context and uses the name of the resource as key.
    /// </summary>
    /// <param name="resource">The resource to add.</param>
    /// <returns>The added resource.</returns>
    public T AddResource<T>(T resource) where T : IResource
    {
        _resources.Add(resource.Name, resource);
        return resource;
    }

    /// <summary>
    /// Gets a resource of type <see cref="Resource{T}"/> by its key.
    /// </summary>
    /// <typeparam name="T">The type of the resource value.</typeparam>
    /// <param name="key">The key of the resource.</param>
    /// <returns>The <see cref="Resource{T}"/> associated with the specified key.</returns>
    public Resource<T> GetResource<T>(string key) => (Resource<T>)_resources[key];

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
    /// <param name="step">The progress step.</param>
    /// <param name="total">The total value to set.</param>
    public void SetTotal(IHasProgress step, long total)
    {
        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
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
    /// <param name="step">The progress step.</param>
    /// <param name="amount">The amount to increment by. Defaults to 1.</param>
    public void IncrementProgress(IHasProgress step, long amount = 1)
    {
        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
        {
            task.Increment(amount);
        }
    }

    /// <summary>
    /// Executes the specified step asynchronously within this context.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask ExecuteAsync(IStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_consoleTasks.TryGetValue(step, out ProgressTask? task))
        {
            task.StartTask();
        }

        await step.ExecuteAsync(this);

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
    /// <param name="message">The error message.</param>
    public void Fail(IStep step, string message)
    {
        _errors.Add((step, message));
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
