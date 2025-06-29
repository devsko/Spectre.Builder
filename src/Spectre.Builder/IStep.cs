// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step in a process that supports progress reporting and asynchronous execution,
/// parameterized by a specific <see cref="BuilderContext{TContext}"/> type.
/// </summary>
/// <typeparam name="TContext">
/// The type of the builder context used for step execution. Must inherit from <see cref="BuilderContext{TContext}"/>.
/// </typeparam>
public interface IStep<TContext> : IHasProgress<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// Prepares the step for execution by setting its position in the progress hierarchy.
    /// </summary>
    /// <param name="context">The context for the step preparation.</param>
    /// <param name="insertAfter">The progress item after which this step should be inserted.</param>
    /// <param name="level">The hierarchical level of the step.</param>
    /// <returns>The progress item representing this step.</returns>
    IHasProgress<TContext> Prepare(TContext context, IHasProgress<TContext>? insertAfter, int level);

    /// <summary>
    /// Executes the step asynchronously using the specified context.
    /// </summary>
    /// <param name="context">The context for the step execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}
