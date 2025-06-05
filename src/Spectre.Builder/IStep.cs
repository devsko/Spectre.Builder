// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a step in a process that supports progress reporting and asynchronous execution.
/// </summary>
public interface IStep : IHasProgress
{
    /// <summary>
    /// Prepares the step for execution using the specified context.
    /// </summary>
    /// <param name="context">The context for the step execution.</param>
    void Prepare(BuilderContext context);

    /// <summary>
    /// Executes the step asynchronously using the specified context.
    /// </summary>
    /// <param name="context">The context for the step execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(BuilderContext context);
}
