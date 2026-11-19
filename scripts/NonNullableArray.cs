using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A Godot <see cref="Array{T}"/> wrapper that rejects null elements.
/// </summary>
/// <typeparam name="T">Variant-compatible element type.</typeparam>
/// <remarks>
/// Empty collections are allowed. Inserting or assigning <c>null</c> is not.
/// </remarks>
public partial class NonNullableArray<[MustBeVariant] T> : RefCounted, IEnumerable<T>
{
	private readonly Array<T> _backer = new();

	/// <summary>
	/// Gets the number of elements in the array.
	/// </summary>
	public int Count => _backer.Count;

	/// <summary>
	/// Gets or sets the element at <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Zero-based index.</param>
	/// <exception cref="ArgumentNullException">Thrown when setting a null value.</exception>
	public T this[int index]
	{
		get => _backer[index];
		set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "NonNullableArray cannot contain null elements.");
            }
            _backer[index] = value;
        }
	}

	/// <summary>
	/// Creates an empty array.
	/// </summary>
	public NonNullableArray()
	{
        _backer = new();
	}

	/// <summary>
	/// Creates an array from <paramref name="items"/>, rejecting any null element.
	/// </summary>
	/// <param name="items">Initial elements. Must not contain null.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null or contains null.</exception>
	public NonNullableArray(IEnumerable<T> items)
	{
        ArgumentNullException.ThrowIfNull(items);
        foreach (var e in items)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(items), "Sequence contains a null element.");
            }
        }
		_backer = new Array<T>(items);
	}

	/// <summary>
	/// Appends <paramref name="item"/> to the end of the array.
	/// </summary>
	/// <param name="item">Non-null element to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
	public void Add(T item)
	{
		ArgumentNullException.ThrowIfNull(item);
        _backer.Add(item);
	}

	/// <summary>
	/// Inserts <paramref name="item"/> at <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Zero-based insertion index.</param>
	/// <param name="item">Non-null element to insert.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
	public void Insert(int index, T item)
	{
        ArgumentNullException.ThrowIfNull(item);
        if (index < 0 || index >= Count)
        {
            throw new IndexOutOfRangeException(nameof(item));
        }
		_backer.Insert(index, item);
	}

	/// <summary>
	/// Removes the first occurrence of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">Element to remove.</param>
	/// <returns><c>true</c> if an element was removed; otherwise <c>false</c>.</returns>
	public bool Remove(T item)
	{
        int num = IndexOf(item);
        if (num > 0)
        {
            _backer.Remove(item);
            return true;
        }
        return false;
	}

	/// <summary>
	/// Removes the element at <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Zero-based index of the element to remove.</param>
	public void RemoveAt(int index)
	{
        if (index < 0 || index >= _backer.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
		_backer.RemoveAt(index);
	}

	/// <summary>
	/// Removes all elements from the array.
	/// </summary>
	public void Clear()
	{
		_backer.Clear();
	}

	/// <summary>
	/// Returns whether <paramref name="item"/> is present in the array.
	/// </summary>
	/// <param name="item">Element to locate.</param>
	public bool Contains(T item)
	{
		return _backer.Contains(item);
	}

	/// <summary>
	/// Returns the index of the first occurrence of <paramref name="item"/>, or <c>-1</c> if not found.
	/// </summary>
	/// <param name="item">Element to locate.</param>
	public int IndexOf(T item)
	{
		return _backer.IndexOf(item);
	}

	/// <summary>
	/// Overwrites every element with <paramref name="item"/>.
	/// </summary>
	/// <param name="item">Non-null fill value.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
	public void Fill(T item)
	{
        ArgumentNullException.ThrowIfNull(item);
		_backer.Fill(item);
	}

	/// <summary>
	/// Returns a shallow copy of the underlying Godot array.
	/// </summary>
	public Array<T> ToGodotArray()
	{
		return new Array<T>(_backer);
	}
    
    public T[] ToSystemArray()
    {
        return _backer.ToArray();
    }

	/// <summary>
	/// Returns an enumerator that iterates over the array.
	/// </summary>
	public IEnumerator<T> GetEnumerator()
	{
		return _backer.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
