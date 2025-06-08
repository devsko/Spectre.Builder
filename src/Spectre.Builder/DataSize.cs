// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Spectre.Builder;

/// <summary>
/// Specifies the unit of measurement for data size.
/// </summary>
public enum DataSizeUnit
{
    /// <summary>
    /// No unit (bytes).
    /// </summary>
    None,
    /// <summary>
    /// Kilobytes (KB).
    /// </summary>
    Kilo,
    /// <summary>
    /// Megabytes (MB).
    /// </summary>
    Mega,
    /// <summary>
    /// Gigabytes (GB).
    /// </summary>
    Giga,
}

/// <summary>
/// Represents a data size with a specific unit.
/// </summary>
public readonly struct DataSize
{
    /// <summary>
    /// Gets the suffixes for each data size unit.
    /// </summary>
    private static string[] Suffixes => ["", "KB", "MB", "GB"];

    /// <summary>
    /// Gets the number of bytes.
    /// </summary>
    public int Bytes { get; }

    /// <summary>
    /// Gets the unit of the data size.
    /// </summary>
    public DataSizeUnit Unit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSize"/> struct with the specified number of bytes.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    public DataSize(int bytes)
    {
        Bytes = bytes;
        Unit = GetUnit(bytes);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSize"/> struct with the specified number of bytes and unit.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <param name="unit">The data size unit.</param>
    public DataSize(int bytes, DataSizeUnit unit)
    {
        Bytes = bytes;
        Unit = unit;
    }

    /// <summary>
    /// Gets the suffix for the current data size unit.
    /// </summary>
    public string Suffix => Unit != 0
        ? Suffixes[(int)Unit]
        : Bytes == 1 ? "byte" : "bytes";

    /// <summary>
    /// Formats the data size as a string using the specified format provider.
    /// </summary>
    /// <param name="formatProvider">The format provider to use, or <see langword="null"/> to use the invariant culture.</param>
    /// <returns>The formatted data size string.</returns>
    public string Format(IFormatProvider? formatProvider = null)
    {
        formatProvider ??= NumberFormatInfo.InvariantInfo;

        return Unit != 0
            ? (Bytes / MathF.Pow(1000, (int)Unit)).ToString("F1", formatProvider)
            : Bytes.ToString(formatProvider);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Format()} {Suffix}";

    private static DataSizeUnit GetUnit(int bytes)
    {
        int divisor = 1_024;
        for (DataSizeUnit unit = 0; unit <= DataSizeUnit.Giga; unit++)
        {
            if (bytes < divisor)
            {
                return unit;
            }
            divisor *= 1_024;
        }

        return DataSizeUnit.Giga;
    }
}
