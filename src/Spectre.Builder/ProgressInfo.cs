// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents progress information for a step, providing context for progress tracking.
/// </summary>
public class ProgressInfo(string name) : IHasProgress
{
    /// <summary>
    /// Gets or sets the parent step associated with this progress information.
    /// </summary>
    public IHasProgress? Parent { get; set; }

    /// <summary>
    /// Gets the name of the progress information.
    /// </summary>
    public string Name => name;

    /// <inheritdoc/>
    bool IHasProgress.ShouldShowProgress => Parent?.ShouldShowProgress ?? throw new InvalidOperationException();

    /// <inheritdoc/>
    ProgressType IHasProgress.Type => ProgressType.ValueRaw;

    /// <inheritdoc/>
    ProgressState IHasProgress.State => Parent?.State ?? throw new InvalidOperationException();
}
