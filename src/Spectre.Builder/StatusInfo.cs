// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public abstract class StatusInfo(string name, ProgressType valueType, Func<long> getValue) : ProgressInfo(name), IHasProgress
{
    public Func<long> GetValue => getValue;

    ProgressType IHasProgress.Type => valueType;
}

public class EmptyInfo() : StatusInfo("", 0, () => 0);

public class MemoryInfo() : StatusInfo("Heap size", ProgressType.ValueDataSize, () => GC.GetTotalMemory(false));

public class GCTimeInfo() : StatusInfo("GC time", ProgressType.ValueTimeSpan, () => GC.GetTotalPauseDuration().Ticks);
