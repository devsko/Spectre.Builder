// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public class ProgressInfo(string name) : IHasProgress
{
    public IStep? Parent { get; set; }

    public string Name => name;

    bool IHasProgress.ShouldShowProgress => Parent?.ShouldShowProgress ?? throw new InvalidOperationException();

    ProgressType IHasProgress.Type => ProgressType.ValueRaw;

    ProgressState IHasProgress.State => Parent?.State ?? throw new InvalidOperationException();
}
