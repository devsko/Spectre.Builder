// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a progress reporter that invokes a specified handler when progress is reported.
/// </summary>
/// <typeparam name="T">
/// The type of progress update value.
/// </typeparam>
public class ProgressSlim<T>(Action<T> handler) : IProgress<T>
{
    /// <inheritdoc/>
    public void Report(T value)
    {
        handler(value);
    }
}
