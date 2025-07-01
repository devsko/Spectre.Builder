// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents an abstract step in a execution pipeline.
/// </summary>
/// <typeparam name="TContext">
/// The type of the builder context used for step execution. Must inherit from <see cref="BuilderContext{TContext}"/>.
/// </typeparam>
public abstract class Step<TContext> : IHasProgress<TContext> where TContext : class, IBuilderContext<TContext>
{
    private sealed class SequentialStepImpl(string name, IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync) : SequentialStep<TContext>(steps, createStepsAsync)
    {
        public override string Name => name;
    }

    private sealed class ParallelStepImpl(string name, IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync, ParallelOptions options) : ParallelStep<TContext>(steps, createStepsAsync)
    {
        public override string Name => name;

        protected override ParallelOptions ParallelOptions => options;
    }

    /// <inheritdoc/>
    public ProgressState State { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the step is hidden.
    /// </summary>
    public virtual bool IsHidden => false;

    /// <summary>
    /// Gets the name of the progress item.
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <inheritdoc/>
    public abstract ProgressType Type { get; }

    /// <inheritdoc/>
    IHasProgress<TContext> IHasProgress<TContext>.SelfOrLastChild => this;

    /// <inheritdoc/>
    public virtual string GetName(TContext context) => Name;

    /// <summary>
    /// Prepares the step for execution by setting its position in the progress hierarchy.
    /// </summary>
    /// <param name="context">The context for the step preparation.</param>
    /// <param name="insertAfter">The progress item after which this step should be inserted.</param>
    /// <param name="level">The hierarchical level of the step.</param>
    /// <returns>The progress item representing this step.</returns>
    protected internal abstract IHasProgress<TContext> Prepare(TContext context, IHasProgress<TContext>? insertAfter, int level);

    /// <summary>
    /// Executes the step asynchronously using the specified context.
    /// </summary>
    /// <param name="context">The context for the step execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected internal abstract Task ExecuteAsync(TContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a sequential step with the specified name and steps.
    /// </summary>
    /// <param name="name">The name of the sequential step.</param>
    /// <param name="steps">The steps to execute sequentially.</param>
    /// <param name="createStepsAsync">An optional function to create steps asynchronously.</param>
    /// <returns>A new <see cref="SequentialStep{TContext}"/> instance.</returns>
    public static SequentialStep<TContext> Sequential(string name, IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync = null) => new SequentialStepImpl(name, steps, createStepsAsync);

    /// <summary>
    /// Creates a parallel step with the specified name, steps, and options.
    /// </summary>
    /// <param name="name">The name of the parallel step.</param>
    /// <param name="steps">The steps to execute in parallel.</param>
    /// <param name="createStepsAsync">An optional function to create steps asynchronously.</param>
    /// <param name="options">The parallel options to use. If null, default options are used.</param>
    /// <returns>A new <see cref="ParallelStep{TContext}"/> instance.</returns>
    public static ParallelStep<TContext> Parallel(string name, IEnumerable<Step<TContext>> steps, Func<CompoundStep<TContext>, TContext, CancellationToken, Task>? createStepsAsync = null, ParallelOptions? options = null) => new ParallelStepImpl(name, steps, createStepsAsync, options ?? new ParallelOptions());
}
