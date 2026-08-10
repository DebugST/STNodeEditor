using System;
using System.Collections;

namespace ST.Library.UI.NodeEditor;

public class STNodeControlCollection : IList, ICollection, IEnumerable
{
	private int _Count;

	private STNodeControl[] m_controls;

	private STNode m_owner;

	public int Count => _Count;

	public bool IsFixedSize => false;

	public bool IsReadOnly => false;

	public STNodeControl this[int index]
	{
		get
		{
			if (index < 0 || index >= _Count)
			{
				throw new IndexOutOfRangeException("索引越界");
			}
			return m_controls[index];
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
			this[index] = (STNodeControl)value;
		}
	}

	int ICollection.Count => _Count;

	bool ICollection.IsSynchronized => IsSynchronized;

	object ICollection.SyncRoot => SyncRoot;

	internal STNodeControlCollection(STNode owner)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("所有者不能为空");
		}
		m_owner = owner;
		m_controls = new STNodeControl[4];
	}

	public int Add(STNodeControl control)
	{
		if (control == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		EnsureSpace(1);
		int num = IndexOf(control);
		if (-1 == num)
		{
			num = _Count;
			control.Owner = m_owner;
			m_controls[_Count++] = control;
			Redraw();
		}
		return num;
	}

	public void AddRange(STNodeControl[] controls)
	{
		if (controls == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		EnsureSpace(controls.Length);
		foreach (STNodeControl sTNodeControl in controls)
		{
			if (sTNodeControl == null)
			{
				throw new ArgumentNullException("添加对象不能为空");
			}
			if (-1 == IndexOf(sTNodeControl))
			{
				sTNodeControl.Owner = m_owner;
				m_controls[_Count++] = sTNodeControl;
			}
		}
		Redraw();
	}

	public void Clear()
	{
		for (int i = 0; i < _Count; i++)
		{
			m_controls[i].Owner = null;
		}
		_Count = 0;
		m_controls = new STNodeControl[4];
		Redraw();
	}

	public bool Contains(STNodeControl option)
	{
		return IndexOf(option) != -1;
	}

	public int IndexOf(STNodeControl option)
	{
		return Array.IndexOf(m_controls, option);
	}

	public void Insert(int index, STNodeControl control)
	{
		if (index < 0 || index >= _Count)
		{
			throw new IndexOutOfRangeException("索引越界");
		}
		if (control == null)
		{
			throw new ArgumentNullException("插入对象不能为空");
		}
		EnsureSpace(1);
		for (int num = _Count; num > index; num--)
		{
			m_controls[num] = m_controls[num - 1];
		}
		control.Owner = m_owner;
		m_controls[index] = control;
		_Count++;
		Redraw();
	}

	public void Remove(STNodeControl control)
	{
		int num = IndexOf(control);
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
		m_controls[index].Owner = null;
		int i = index;
		for (int count = _Count; i < count; i++)
		{
			m_controls[i] = m_controls[i + 1];
		}
		Redraw();
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("数组不能为空");
		}
		m_controls.CopyTo(array, index);
	}

	public IEnumerator GetEnumerator()
	{
		int i = 0;
		for (int Len = _Count; i < Len; i++)
		{
			yield return m_controls[i];
		}
	}

	private void EnsureSpace(int elements)
	{
		if (elements + _Count > m_controls.Length)
		{
			STNodeControl[] array = new STNodeControl[Math.Max(m_controls.Length * 2, elements + _Count)];
			m_controls.CopyTo(array, 0);
			m_controls = array;
		}
	}

	protected void Redraw()
	{
		if (m_owner != null && m_owner.Owner != null)
		{
			m_owner.Owner.Invalidate(m_owner.Owner.CanvasToControl(m_owner.Rectangle));
		}
	}

	int IList.Add(object value)
	{
		return Add((STNodeControl)value);
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		return Contains((STNodeControl)value);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf((STNodeControl)value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, (STNodeControl)value);
	}

	void IList.Remove(object value)
	{
		Remove((STNodeControl)value);
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
}
