// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step in a execution pipeline.
/// </summary>
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

    /// <summary>
    /// Creates a sequential step with the specified name and steps.
    /// </summary>
    /// <param name="name">The name of the sequential step.</param>
    /// <param name="steps">The steps to execute sequentially.</param>
    /// <returns>A new <see cref="SequentialStep"/> instance.</returns>
    public static SequentialStep Sequential(string name, IEnumerable<IStep> steps) => new SequentialStepImpl(name, steps);

    /// <summary>
    /// Creates a parallel step with the specified name, steps, and options.
    /// </summary>
    /// <param name="name">The name of the parallel step.</param>
    /// <param name="steps">The steps to execute in parallel.</param>
    /// <param name="options">The parallel options to use. If null, default options are used.</param>
    /// <returns>A new <see cref="ParallelStep"/> instance.</returns>
    public static ParallelStep Parallel(string name, IEnumerable<IStep> steps, ParallelOptions? options = null) => new ParallelStepImpl(name, steps, options ?? new ParallelOptions());

    /// <inheritdoc/>
    public ProgressState State { get; protected set; }

    /// <inheritdoc/>
    public virtual string Name => GetType().Name;

    /// <inheritdoc/>
    public virtual bool ShouldShowProgress => true;

    ProgressType IHasProgress.Type => throw new NotImplementedException();

    void IStep.Prepare(BuilderContext context) => throw new NotImplementedException();

    Task IStep.ExecuteAsync(BuilderContext context) => throw new NotImplementedException();
}
