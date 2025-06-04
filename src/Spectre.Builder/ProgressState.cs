// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents the state of a progress operation.
/// </summary>
public enum ProgressState
{
    /// <summary>
    /// The operation is waiting to start.
    /// </summary>
    Wait,
    /// <summary>
    /// The operation is currently running.
    /// </summary>
    Running,
    /// <summary>
    /// The operation has completed.
    /// </summary>
    Done,
    /// <summary>
    /// The operation was skipped.
    /// </summary>
    Skip,
}
