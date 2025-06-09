// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step in a execution pipeline.
/// </summary>
public abstract class Step<TContext> : IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    private sealed class SequentialStepImpl(string name, IEnumerable<IStep<TContext>> steps) : SequentialStep<TContext>(steps)
    {
        public override string Name => name;
    }

    private sealed class ParallelStepImpl(string name, IEnumerable<IStep<TContext>> steps, ParallelOptions options) : ParallelStep<TContext>(steps)
    {
        public override string Name => name;

        protected override ParallelOptions ParallelOptions => options;
    }

    /// <summary>
    /// Creates a sequential step with the specified name and steps.
    /// </summary>
    /// <param name="name">The name of the sequential step.</param>
    /// <param name="steps">The steps to execute sequentially.</param>
    /// <returns>A new <see cref="SequentialStep{TContext}"/> instance.</returns>
    public static SequentialStep<TContext> Sequential(string name, IEnumerable<IStep<TContext>> steps) => new SequentialStepImpl(name, steps);

    /// <summary>
    /// Creates a parallel step with the specified name, steps, and options.
    /// </summary>
    /// <param name="name">The name of the parallel step.</param>
    /// <param name="steps">The steps to execute in parallel.</param>
    /// <param name="options">The parallel options to use. If null, default options are used.</param>
    /// <returns>A new <see cref="ParallelStep{TContext}"/> instance.</returns>
    public static ParallelStep<TContext> Parallel(string name, IEnumerable<IStep<TContext>> steps, ParallelOptions? options = null) => new ParallelStepImpl(name, steps, options ?? new ParallelOptions());

    /// <inheritdoc/>
    public ProgressState State { get; protected set; }

    /// <inheritdoc/>
    public virtual string Name => GetType().Name;

    /// <inheritdoc/>
    public virtual bool ShouldShowProgress => true;

    ProgressType IHasProgress.Type => throw new NotImplementedException();

    void IStep<TContext>.Prepare(TContext context) => throw new NotImplementedException();

    Task IStep<TContext>.ExecuteAsync(TContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
}
