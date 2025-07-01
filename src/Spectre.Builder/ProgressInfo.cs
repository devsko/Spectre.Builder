// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents progress information for a step, providing context for progress tracking.
/// </summary>
public class ProgressInfo<TContext>(string name) : IHasProgress<TContext> where TContext : BuilderContext<TContext>
{
    /// <inheritdoc/>
    ProgressType IHasProgress<TContext>.Type => ProgressType.ValueRaw;

    /// <inheritdoc/>
    ProgressState IHasProgress<TContext>.State => ProgressState.Running;

    /// <inheritdoc/>
    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => this;

    /// <inheritdoc/>
    string IHasProgress<TContext>.GetName(TContext context) => name;
}
