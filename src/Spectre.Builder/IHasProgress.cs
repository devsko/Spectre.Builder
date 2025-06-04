// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public interface IHasProgress
{
    string Name { get; }
    bool ShouldShowProgress { get; }
    ProgressType Type { get; }
    ProgressState State { get; }
}
