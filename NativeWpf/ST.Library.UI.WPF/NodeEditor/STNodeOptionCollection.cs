using System;
using System.Collections;
using System.Collections.Generic;

namespace ST.Library.UI.NodeEditor;

public class STNodeOptionCollection : IList, ICollection, IEnumerable
{
	private int _Count;

	private STNodeOption[] m_options;

	private STNode m_owner;

	private bool m_isInput;

	public int Count => _Count;

	public bool IsFixedSize => false;

	public bool IsReadOnly => false;

	public STNodeOption this[int index]
	{
		get
		{
			if (index < 0 || index >= _Count)
			{
				throw new IndexOutOfRangeException("索引越界");
			}
			return m_options[index];
		}
		set
		{
			throw new InvalidOperationException("禁止重新赋值元素");
		}
	}

	public bool IsSynchronized => true;

	public object SyncRoot => this;

	bool IList.IsFixedSize => IsFixedSize;

	bool IList.IsReadOnly => IsReadOnly;

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = (STNodeOption)value;
		}
	}

	int ICollection.Count => _Count;

	bool ICollection.IsSynchronized => IsSynchronized;

	object ICollection.SyncRoot => SyncRoot;

	internal STNodeOptionCollection(STNode owner, bool isInput)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("所有者不能为空");
		}
		m_owner = owner;
		m_isInput = isInput;
		m_options = new STNodeOption[4];
	}

	public STNodeOption Add(string strText, Type dataType, bool bSingle)
	{
		int num = Add(new STNodeOption(strText, dataType, bSingle));
		return m_options[num];
	}

	public int Add(STNodeOption option)
	{
		if (option == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		EnsureSpace(1);
		int num = ((option == STNodeOption.Empty) ? (-1) : IndexOf(option));
		if (-1 == num)
		{
			num = _Count;
			option.Owner = m_owner;
			option.IsInput = m_isInput;
			m_options[_Count++] = option;
			Invalidate();
		}
		return num;
	}

	public void AddRange(STNodeOption[] options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		EnsureSpace(options.Length);
		foreach (STNodeOption sTNodeOption in options)
		{
			if (sTNodeOption == null)
			{
				throw new ArgumentNullException("添加对象不能为空");
			}
			if (-1 == IndexOf(sTNodeOption))
			{
				sTNodeOption.Owner = m_owner;
				sTNodeOption.IsInput = m_isInput;
				m_options[_Count++] = sTNodeOption;
			}
		}
		Invalidate();
	}

	public void Clear()
	{
		for (int i = 0; i < _Count; i++)
		{
			m_options[i].Owner = null;
		}
		_Count = 0;
		m_options = new STNodeOption[4];
		Invalidate();
	}

	public bool Contains(STNodeOption option)
	{
		return IndexOf(option) != -1;
	}

	public int IndexOf(STNodeOption option)
	{
		return Array.IndexOf(m_options, option, 0, _Count);
	}

	public void Insert(int index, STNodeOption option)
	{
		if (index < 0 || index > _Count)
		{
			throw new IndexOutOfRangeException("索引越界");
		}
		if (option == null)
		{
			throw new ArgumentNullException("插入对象不能为空");
		}
		if (option != STNodeOption.Empty && IndexOf(option) >= 0)
		{
			return;
		}
		EnsureSpace(1);
		for (int num = _Count; num > index; num--)
		{
			m_options[num] = m_options[num - 1];
		}
		option.Owner = m_owner;
		option.IsInput = m_isInput;
		m_options[index] = option;
		_Count++;
		Invalidate();
	}

	public void Remove(STNodeOption option)
	{
		int num = IndexOf(option);
		if (num != -1)
		{
			RemoveAt(num);
		}
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= _Count)
		{
			throw new IndexOutOfRangeException("索引越界");
		}
		_Count--;
		m_options[index].Owner = null;
		int i = index;
		for (int count = _Count; i < count; i++)
		{
			m_options[i] = m_options[i + 1];
		}
		m_options[_Count] = null;
		Invalidate();
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("数组不能为空");
		}
		m_options.CopyTo(array, index);
	}

	public IEnumerator GetEnumerator()
	{
		int i = 0;
		for (int Len = _Count; i < Len; i++)
		{
			yield return m_options[i];
		}
	}

	private void EnsureSpace(int elements)
	{
		if (elements + _Count > m_options.Length)
		{
			STNodeOption[] array = new STNodeOption[Math.Max(m_options.Length * 2, elements + _Count)];
			m_options.CopyTo(array, 0);
			m_options = array;
		}
	}

	protected void Invalidate()
	{
		if (m_owner != null && m_owner.Owner != null)
		{
			m_owner.BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: true);
		}
	}

	int IList.Add(object value)
	{
		return Add((STNodeOption)value);
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		return Contains((STNodeOption)value);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf((STNodeOption)value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, (STNodeOption)value);
	}

	void IList.Remove(object value)
	{
		Remove((STNodeOption)value);
	}

	void IList.RemoveAt(int index)
	{
		RemoveAt(index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CopyTo(array, index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public STNodeOption[] ToArray()
	{
		STNodeOption[] array = new STNodeOption[_Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = m_options[i];
		}
		return array;
	}

	internal bool Reorder(IReadOnlyList<STNodeOption> orderedOptions)
	{
		if (orderedOptions == null)
		{
			throw new ArgumentNullException(nameof(orderedOptions));
		}
		if (orderedOptions.Count != _Count)
		{
			return false;
		}

		HashSet<STNodeOption> seen = new HashSet<STNodeOption>();
		for (int i = 0; i < orderedOptions.Count; i++)
		{
			STNodeOption option = orderedOptions[i];
			if (option == null || IndexOf(option) == -1 || !seen.Add(option))
			{
				return false;
			}
		}

		bool changed = false;
		for (int i = 0; i < _Count; i++)
		{
			if (!ReferenceEquals(m_options[i], orderedOptions[i]))
			{
				changed = true;
				break;
			}
		}
		if (!changed)
		{
			return false;
		}

		for (int i = 0; i < _Count; i++)
		{
			m_options[i] = orderedOptions[i];
		}
		Invalidate();
		return true;
	}
}
