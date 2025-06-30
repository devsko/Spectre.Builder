// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an item that exposes progress information.
/// </summary>
public interface IHasProgress<TContext> where TContext : class, IBuilderContext<TContext>
{
    /// <summary>
    /// Gets a value indicating whether the progress item is hidden.
    /// </summary>
    bool IsHidden { get; }

    /// <summary>
    /// Gets the name of the progress item.
    /// </summary>
    string GetName(TContext context);

    /// <summary>
    /// Gets the type of progress.
    /// </summary>
    ProgressType Type { get; }

    /// <summary>
    /// Gets the current state of the progress.
    /// </summary>
    ProgressState State { get; }

    /// <summary>
    /// Gets the last progress item in the hierarchy, including itself or its children.
    /// </summary>
    IHasProgress<TContext> SelfOrLastChild { get; }
}
