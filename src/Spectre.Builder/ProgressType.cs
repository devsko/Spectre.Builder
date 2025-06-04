// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Specifies the type of progress representation.
/// </summary>
[Flags]
public enum ProgressType
{
    /// <summary>
    /// Progress is represented as a numeric percentage.
    /// </summary>
    NumericPercentage = 0x01,

    /// <summary>
    /// Progress is represented as a numeric step value.
    /// </summary>
    NumericStep = 0x02,

    /// <summary>
    /// Mask for numeric progress types.
    /// </summary>
    NumericMask = NumericPercentage | NumericStep,

    /// <summary>
    /// Indicates that elapsed time should be visible.
    /// </summary>
    ElapsedVisible = 0x04,

    /// <summary>
    /// Mask for elapsed time visibility.
    /// </summary>
    ElapsedMask = ElapsedVisible,

    /// <summary>
    /// Progress value is represented as a data size.
    /// </summary>
    ValueDataSize = 0x08,

    /// <summary>
    /// Progress value is represented as a raw value.
    /// </summary>
    ValueRaw = 0x10,

    /// <summary>
    /// Progress value is represented as a time span.
    /// </summary>
    ValueTimeSpan = 0x18,

    /// <summary>
    /// Mask for value representation types.
    /// </summary>
    ValueMask = ValueDataSize | ValueRaw | ValueTimeSpan,
}
