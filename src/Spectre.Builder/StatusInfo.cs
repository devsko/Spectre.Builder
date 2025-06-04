// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents status information with a name, progress type, and a value provider.
/// </summary>
/// <param name="name">The name of the status info.</param>
/// <param name="valueType">The type of progress value.</param>
/// <param name="getValue">A function to get the current value.</param>
public abstract class StatusInfo(string name, ProgressType valueType, Func<long> getValue) : ProgressInfo(name), IHasProgress
{
    /// <summary>
    /// Gets the function that provides the current value.
    /// </summary>
    public Func<long> GetValue => getValue;

    /// <summary>
    /// Gets the type of progress value.
    /// </summary>
    ProgressType IHasProgress.Type => valueType;
}

/// <summary>
/// Represents an empty status info.
/// </summary>
public class EmptyInfo() : StatusInfo("", 0, () => 0);

/// <summary>
/// Represents status info for heap memory size.
/// </summary>
public class MemoryInfo() : StatusInfo("Heap size", ProgressType.ValueDataSize, () => GC.GetTotalMemory(false));

/// <summary>
/// Represents status info for total GC pause duration.
/// </summary>
public class GCTimeInfo() : StatusInfo("GC time", ProgressType.ValueTimeSpan, () => GC.GetTotalPauseDuration().Ticks);
