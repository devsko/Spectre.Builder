// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public abstract class Step : IStep
{
    private sealed class SequentialStepImpl(string name, IEnumerable<IStep> steps) : SequentialStep(steps)
    {
        public override string Name => name;
    }

    private sealed class ParallelStepImpl(string name, IEnumerable<IStep> steps, ParallelOptions options) : ParallelStep(steps)
    {
        public override string Name => name;
        protected override ParallelOptions ParallelOptions => options;
    }

    public static SequentialStep Sequential(string name, IEnumerable<IStep> steps) => new SequentialStepImpl(name, steps);

    public static ParallelStep Parallel(string name, IEnumerable<IStep> steps, ParallelOptions? options = null) => new ParallelStepImpl(name, steps, options ?? new ParallelOptions());

    public ProgressState State { get; protected set; }

    public virtual string Name => GetType().Name;

    public virtual bool ShouldShowProgress => true;

    ProgressType IHasProgress.Type => throw new NotImplementedException();

    void IStep.Prepare(StepContext context) => throw new NotImplementedException();

    Task IStep.ExecuteAsync(StepContext context) => throw new NotImplementedException();
}
