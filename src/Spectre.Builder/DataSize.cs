// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

using System.Globalization;

namespace Spectre.Builder;

public enum DataSizeUnit
{
    None,
    Kilo,
    Mega,
    Giga,
}

public readonly struct DataSize
{
    private static string[] Suffixes => ["", "KB", "MB", "GB"];

    public int Bytes { get; }

    public DataSizeUnit Unit { get; }

    public DataSize(int bytes)
    {
        Bytes = bytes;
        Unit = GetUnit(bytes);
    }

    public DataSize(int bytes, DataSizeUnit unit)
    {
        Bytes = bytes;
        Unit = unit;
    }

    public string Suffix => Unit != 0
        ? Suffixes[(int)Unit]
        : Bytes == 1 ? "byte" : "bytes";

    public string Format(IFormatProvider? formatProvider = null)
    {
        formatProvider ??= NumberFormatInfo.InvariantInfo;

        return Unit != 0
            ? (Bytes / MathF.Pow(1000, (int)Unit)).ToString("F1", formatProvider)
            : Bytes.ToString(formatProvider);
    }

    public override string ToString() => $"{Format()} {Suffix}";

    private static DataSizeUnit GetUnit(int bytes)
    {
        int divisor = 1_000;
        for (DataSizeUnit unit = 0; unit <= DataSizeUnit.Giga; unit++)
        {
            if (bytes < divisor)
            {
                return unit;
            }
            divisor *= 1_000;
        }

        return DataSizeUnit.Giga;
    }
}
