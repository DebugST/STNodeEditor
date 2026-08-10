using System;
using System.Collections;

namespace ST.Library.UI.NodeEditor;

public class STNodeCollection : IList, ICollection, IEnumerable
{
	private int _Count;

	private STNode[] m_nodes;

	private STNodeEditor m_owner;

	public int Count => _Count;

	public bool IsFixedSize => false;

	public bool IsReadOnly => false;

	public STNode this[int nIndex]
	{
		get
		{
			if (nIndex < 0 || nIndex >= _Count)
			{
				throw new IndexOutOfRangeException("索引越界");
			}
			return m_nodes[nIndex];
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
			this[index] = (STNode)value;
		}
	}

	int ICollection.Count => _Count;

	bool ICollection.IsSynchronized => IsSynchronized;

	object ICollection.SyncRoot => SyncRoot;

	internal STNodeCollection(STNodeEditor owner)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("所有者不能为空");
		}
		m_owner = owner;
		m_nodes = new STNode[4];
	}

	public void MoveToEnd(STNode node)
	{
		if (_Count < 1 || m_nodes[_Count - 1] == node)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < _Count - 1; i++)
		{
			if (m_nodes[i] == node)
			{
				flag = true;
			}
			if (flag)
			{
				m_nodes[i] = m_nodes[i + 1];
			}
		}
		m_nodes[_Count - 1] = node;
	}

	public int Add(STNode node)
	{
		if (node == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		using STNodeEditTransaction transaction = m_owner.BeginEditTransaction("添加节点");
		int num = IndexOf(node);
		if (-1 == num)
		{
			EnsureSpace(1);
			num = _Count;
			node.Owner = m_owner;
			m_nodes[_Count++] = node;
			m_owner.RecordNodeAdded(node, num);
			m_owner.BuildBounds();
			m_owner.OnNodeAdded(new STNodeEditorEventArgs(node));
			m_owner.Invalidate();
		}
		return num;
	}

	public void AddRange(STNode[] nodes)
	{
		if (nodes == null)
		{
			throw new ArgumentNullException("添加对象不能为空");
		}
		using STNodeEditTransaction transaction = m_owner.BeginEditTransaction("添加节点");
		foreach (STNode sTNode in nodes)
		{
			if (sTNode == null)
			{
				throw new ArgumentNullException("添加对象不能为空");
			}
			Add(sTNode);
		}
	}

	public void Clear()
	{
		if (_Count == 0)
		{
			return;
		}
		using STNodeEditTransaction transaction = m_owner.BeginEditTransaction("清空画布");
		while (_Count > 0)
		{
			RemoveAt(_Count - 1);
		}
	}

	public bool Contains(STNode node)
	{
		return IndexOf(node) != -1;
	}

	public int IndexOf(STNode node)
	{
		return Array.IndexOf(m_nodes, node);
	}

	public void Insert(int nIndex, STNode node)
	{
		if (nIndex < 0 || nIndex > _Count)
		{
			throw new IndexOutOfRangeException("索引越界");
		}
		if (node == null)
		{
			throw new ArgumentNullException("插入对象不能为空");
		}
		using STNodeEditTransaction transaction = m_owner.BeginEditTransaction("添加节点");
		int existingIndex = IndexOf(node);
		if (existingIndex >= 0)
		{
			return;
		}
		EnsureSpace(1);
		for (int num = _Count; num > nIndex; num--)
		{
			m_nodes[num] = m_nodes[num - 1];
		}
		node.Owner = m_owner;
		m_nodes[nIndex] = node;
		_Count++;
		m_owner.RecordNodeAdded(node, nIndex);
		m_owner.OnNodeAdded(new STNodeEditorEventArgs(node));
		m_owner.Invalidate();
		m_owner.BuildBounds();
	}

	public void Remove(STNode node)
	{
		int num = IndexOf(node);
		if (num != -1)
		{
			RemoveAt(num);
		}
	}

	public void RemoveAt(int nIndex)
	{
		if (nIndex < 0 || nIndex >= _Count)
		{
			throw new IndexOutOfRangeException("索引越界");
		}
		using STNodeEditTransaction transaction = m_owner.BeginEditTransaction("删除节点");
		STNode node = m_nodes[nIndex];
		var nodeState = m_owner.CaptureNodeStateForRemoval(node);
		var connectionOperations = m_owner.CaptureNodeConnectionsForRemoval(node);
		bool lockOption = node.LockOption;
		node.LockOption = false;
		try
		{
			using (m_owner.SuspendHistoryRecording())
			{
				node.Owner = null;
			}
		}
		finally
		{
			node.LockOption = lockOption;
		}
		m_owner.InternalRemoveSelectedNode(node);
		if (m_owner.ActiveNode == node)
		{
			m_owner.SetActiveNode(null);
		}
		m_owner.RecordNodeConnectionsRemoved(connectionOperations);
		m_owner.RecordNodeRemoved(node, nIndex, nodeState);
		m_owner.OnNodeRemoved(new STNodeEditorEventArgs(node));
		_Count--;
		int i = nIndex;
		for (int count = _Count; i < count; i++)
		{
			m_nodes[i] = m_nodes[i + 1];
		}
		m_nodes[_Count] = null;
		if (_Count == 0)
		{
			m_owner.ScaleCanvas(1f, 0f, 0f);
			m_owner.MoveCanvas(10f, 10f, bAnimation: true, CanvasMoveArgs.All);
		}
		else
		{
			m_owner.Invalidate();
			m_owner.BuildBounds();
		}
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("数组不能为空");
		}
		m_nodes.CopyTo(array, index);
	}

	public IEnumerator GetEnumerator()
	{
		int i = 0;
		for (int Len = _Count; i < Len; i++)
		{
			yield return m_nodes[i];
		}
	}

	private void EnsureSpace(int elements)
	{
		if (elements + _Count > m_nodes.Length)
		{
			STNode[] array = new STNode[Math.Max(m_nodes.Length * 2, elements + _Count)];
			m_nodes.CopyTo(array, 0);
			m_nodes = array;
		}
	}

	int IList.Add(object value)
	{
		return Add((STNode)value);
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		return Contains((STNode)value);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf((STNode)value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, (STNode)value);
	}

	void IList.Remove(object value)
	{
		Remove((STNode)value);
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

	public STNode[] ToArray()
	{
		STNode[] array = new STNode[_Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = m_nodes[i];
		}
		return array;
	}
}
