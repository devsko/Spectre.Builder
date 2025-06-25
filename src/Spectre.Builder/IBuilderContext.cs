// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Defines the contract for a builder context that manages the execution and progress tracking
/// of steps within a build process. This interface provides methods for adding steps and progress
/// information, executing steps asynchronously, handling failures, and updating progress state.
/// </summary>
/// <typeparam name="TContext">
/// The type of the builder context, which must implement <see cref="IBuilderContext{TContext}"/>.
/// </typeparam>
public interface IBuilderContext<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// Gets or sets the current execution level within the build process.
    /// This value can be used to track the nesting or depth of steps.
    /// </summary>
    int CurrentLevel { get; set; }

    /// <summary>
    /// Adds a <see cref="ProgressInfo{TContext}"/> instance to the context for progress tracking.
    /// </summary>
    /// <param name="progress">The progress information to add.</param>
    void AddProgress(ProgressInfo<TContext> progress);

    /// <summary>
    /// Adds a step to the context for execution and progress tracking.
    /// </summary>
    /// <param name="step">The step to add.</param>
    void AddStep(IStep<TContext> step);

    /// <summary>
    /// Retrieves the progress information and its associated nesting level for a given identifier.
    /// </summary>
    /// <param name="id">The identifier of the progress item.</param>
    /// <returns>
    /// A tuple containing the <see cref="IHasProgress{TContext}"/> instance and its nesting level.
    /// </returns>
    (IHasProgress<TContext>, int) GetProgressAndLevel(int id);

    /// <summary>
    /// Executes the specified step asynchronously within the context.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the step.
    /// </returns>
    Task ExecuteAsync(IStep<TContext> step, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the specified step as failed and records the associated error message.
    /// </summary>
    /// <param name="step">The step that failed.</param>
    /// <param name="error">The error message describing the failure.</param>
    void Fail(IStep<TContext> step, string error);

    /// <summary>
    /// Ensures that the context is in a valid state for further execution.
    /// Throws an exception if the context is invalid.
    /// </summary>
    void EnsureValid();

    /// <summary>
    /// Sets the total value for the specified progress item.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="total">The total value to set.</param>
    void SetTotal(IHasProgress<TContext> progress, long total);

    /// <summary>
    /// Sets the current progress value for the specified progress item.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="value">The progress value to set.</param>
    void SetProgress(IHasProgress<TContext> progress, long value);

    /// <summary>
    /// Increments the progress value for the specified progress item by the given amount.
    /// </summary>
    /// <param name="progress">The progress item to update.</param>
    /// <param name="amount">The amount to increment the progress by. Defaults to 1.</param>
    void IncrementProgress(IHasProgress<TContext> progress, long amount = 1);
}
