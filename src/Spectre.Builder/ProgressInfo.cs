// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents progress information for a step, providing context for progress tracking.
/// </summary>
public class ProgressInfo<TContext>(string name) : IHasProgress<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// Gets or sets the parent step associated with this progress information.
    /// </summary>
    public IHasProgress<TContext>? Parent { get; set; }

    /// <summary>
    /// Gets the name of the progress information.
    /// </summary>
    public string GetName(TContext context) => name;

    /// <inheritdoc/>
    bool IHasProgress<TContext>.ShouldShowProgress => Parent?.ShouldShowProgress ?? throw new InvalidOperationException();

    /// <inheritdoc/>
    ProgressType IHasProgress<TContext>.Type => ProgressType.ValueRaw;

    /// <inheritdoc/>
    ProgressState IHasProgress<TContext>.State => Parent?.State ?? throw new InvalidOperationException();
}
