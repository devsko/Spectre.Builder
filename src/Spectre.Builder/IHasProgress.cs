// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an item that exposes progress information.
/// </summary>
public interface IHasProgress
{
    /// <summary>
    /// Gets the name of the progress item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether progress should be shown.
    /// </summary>
    bool ShouldShowProgress { get; }

    /// <summary>
    /// Gets the type of progress.
    /// </summary>
    ProgressType Type { get; }

    /// <summary>
    /// Gets the current state of the progress.
    /// </summary>
    ProgressState State { get; }
}
