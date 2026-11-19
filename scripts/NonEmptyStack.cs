using Godot;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// A last-in, first-out stack that is constructed with at least one element and never becomes empty.
/// </summary>
/// <typeparam name="T">Element type stored in the stack.</typeparam>
/// <remarks>
/// The constructor requires an initial element so the stack starts non-empty.
/// <see cref="Pop"/> refuses to remove the final element, preserving that invariant.
/// </remarks>
public partial class NonEmptyStack<T> : RefCounted
{
    /// <summary>
    /// Gets the number of elements currently on the stack.
    /// </summary>
    /// <remarks>
    /// Always at least <c>1</c> after construction.
    /// </remarks>
    public int Count => _backer.Count;

    private readonly Stack<T> _backer = new();

    /// <summary>
    /// Creates a stack seeded with <paramref name="initialElement"/>.
    /// </summary>
    /// <param name="initialElement">The first element on the stack. After construction, <see cref="Count"/> is <c>1</c>.</param>
    public NonEmptyStack(T initialElement)
    {
        _backer.Push(initialElement);
    }

    /// <summary>
    /// Pushes <paramref name="item"/> onto the top of the stack.
    /// </summary>
    /// <param name="item">Element to place on top.</param>
    public void Push(T item)
    {
        _backer.Push(item);
    }

    /// <summary>
    /// Removes and returns the top element, unless that would empty the stack.
    /// </summary>
    /// <returns>
    /// The former top element when <see cref="Count"/> is greater than <c>1</c>;
    /// otherwise the sole remaining element, which stays on the stack.
    /// </returns>
    /// <remarks>
    /// When <see cref="Count"/> is <c>1</c>, this behaves like <see cref="Peek"/> and does not change <see cref="Count"/>.
    /// </remarks>
    public T Pop()
    {
        if (_backer.Count == 1)
        {
            return _backer.Peek();
        }
        return _backer.Pop();
    }

    /// <summary>
    /// Returns the element at the top of the stack without removing it.
    /// </summary>
    /// <returns>The current top element.</returns>
    public T Peek()
    {
        return _backer.Peek();
    }
}
