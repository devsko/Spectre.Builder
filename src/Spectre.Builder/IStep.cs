// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public interface IStep : IHasProgress
{
    void Prepare(StepContext context);
    Task ExecuteAsync(StepContext context);
}
