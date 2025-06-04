// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public enum ProgressType
{
    NumericPercentage = 0x01,
    NumericStep = 0x02,
    NumericMask = NumericPercentage | NumericStep,
    ElapsedVisible = 0x04,
    ElapsedMask = ElapsedVisible,
    ValueDataSize = 0x08,
    ValueRaw = 0x10,
    ValueTimeSpan = 0x18,
    ValueMask = ValueDataSize | ValueRaw | ValueTimeSpan,
}
