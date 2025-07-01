// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step in a execution pipeline.
/// </summary>
public abstract class Step<TContext> : IStep<TContext> where TContext : class, IBuilderContext<TContext>
{
    private sealed class SequentialStepImpl(string name, IEnumerable<IStep<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync) : SequentialStep<TContext>(steps, createStepsAsync)
    {
        public override string Name => name;
    }

    private sealed class ParallelStepImpl(string name, IEnumerable<IStep<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync, ParallelOptions options) : ParallelStep<TContext>(steps, createStepsAsync)
    {
        public override string Name => name;

        protected override ParallelOptions ParallelOptions => options;
    }

    /// <summary>
    /// Creates a sequential step with the specified name and steps.
    /// </summary>
    /// <param name="name">The name of the sequential step.</param>
    /// <param name="steps">The steps to execute sequentially.</param>
    /// <param name="createStepsAsync">An optional function to create steps asynchronously.</param>
    /// <returns>A new <see cref="SequentialStep{TContext}"/> instance.</returns>
    public static SequentialStep<TContext> Sequential(string name, IEnumerable<IStep<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync = null) => new SequentialStepImpl(name, steps, createStepsAsync);

    /// <summary>
    /// Creates a parallel step with the specified name, steps, and options.
    /// </summary>
    /// <param name="name">The name of the parallel step.</param>
    /// <param name="steps">The steps to execute in parallel.</param>
    /// <param name="createStepsAsync">An optional function to create steps asynchronously.</param>
    /// <param name="options">The parallel options to use. If null, default options are used.</param>
    /// <returns>A new <see cref="ParallelStep{TContext}"/> instance.</returns>
    public static ParallelStep<TContext> Parallel(string name, IEnumerable<IStep<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync = null, ParallelOptions? options = null) => new ParallelStepImpl(name, steps, createStepsAsync, options ?? new ParallelOptions());

    /// <inheritdoc/>
    bool IStep<TContext>.IsHidden => false;

    /// <inheritdoc/>
    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => this;

    /// <inheritdoc/>
    public ProgressState State { get; protected set; }

    /// <summary>
    /// Gets the name of the progress item.
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <inheritdoc/>
    public virtual string GetName(TContext context) => Name;

    ProgressType IHasProgress<TContext>.Type => throw new NotImplementedException();

    IHasProgress<TContext> IStep<TContext>.Prepare(TContext context, IHasProgress<TContext>? insertAfter, int level) => throw new NotImplementedException();

    Task IStep<TContext>.ExecuteAsync(TContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
}
