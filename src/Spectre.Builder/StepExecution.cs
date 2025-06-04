// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Specifies the execution necessity of a build step.
/// </summary>
public enum StepExecution
{
    /// <summary>
    /// The step is redundant and does not need to be executed.
    /// </summary>
    Redundant,

    /// <summary>
    /// The step is recommended but not strictly necessary.
    /// </summary>
    Recommended,

    /// <summary>
    /// The step is necessary and must be executed.
    /// </summary>
    Necessary,
}
