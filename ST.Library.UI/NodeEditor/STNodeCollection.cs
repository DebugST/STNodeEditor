using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Drawing;

namespace ST.Library.UI.NodeEditor
{
    public class STNodeCollection : IList, ICollection, IEnumerable
    {
        private int _Count;
        public int Count { get { return _Count; } }
        private STNode[] m_nodes;
        private STNodeEditor m_owner;

        internal STNodeCollection(STNodeEditor owner) {
            if (owner == null) throw new ArgumentNullException("所有者不能为空");
            m_owner = owner;
            m_nodes = new STNode[4];
        }

        public void MoveToEnd(STNode node) {
            if (this._Count < 1) return;
            if (m_nodes[this._Count - 1] == node) return;
            bool bFound = false;
            for (int i = 0; i < _Count - 1; i++) {
                if (m_nodes[i] == node) {
                    bFound = true;
                }
                if (bFound) m_nodes[i] = m_nodes[i + 1];
            }
            m_nodes[this._Count - 1] = node;
        }

        public int Add(STNode node) {
            if (node == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(1);
            int nIndex = this.IndexOf(node);
            if (-1 == nIndex) {
                nIndex = this._Count;
                node.Owner = m_owner;
                //node.BuildSize(true, true, false);
                m_nodes[this._Count++] = node;
                m_owner.BuildBounds();
                m_owner.OnNodeAdded(new STNodeEditorEventArgs(node));
                m_owner.Invalidate();
                //m_owner.Invalidate(m_owner.CanvasToControl(new Rectangle(node.Left - 5, node.Top - 5, node.Width + 10, node.Height + 10)));
                //Console.WriteLine(node.Rectangle);
            }
            return nIndex;
        }

        public void AddRange(STNode[] nodes) {
            if (nodes == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(nodes.Length);
            foreach (var n in nodes) {
                if (n == null) throw new ArgumentNullException("添加对象不能为空");
                if (-1 == this.IndexOf(n)) {
                    n.Owner = m_owner;
                    m_nodes[this._Count++] = n;
                }
                m_owner.OnNodeAdded(new STNodeEditorEventArgs(n));
            }
            m_owner.Invalidate();
            m_owner.BuildBounds();
        }

        public void Clear() {
            for (int i = 0; i < this._Count; i++) {
                m_nodes[i].Owner = null;
                foreach (STNodeOption op in m_nodes[i].InputOptions) op.DisConnectionAll();
                foreach (STNodeOption op in m_nodes[i].OutputOptions) op.DisConnectionAll();
                m_owner.OnNodeRemoved(new STNodeEditorEventArgs(m_nodes[i]));
                m_owner.InternalRemoveSelectedNode(m_nodes[i]);
            }
            this._Count = 0;
            m_nodes = new STNode[4];
            m_owner.SetActiveNode(null);
            m_owner.BuildBounds();
            m_owner.ScaleCanvas(1, 0, 0);       //当不存在节点时候 坐标系回归
            m_owner.MoveCanvas(10, 10, true, CanvasMoveArgs.All);
            m_owner.Invalidate();               //如果画布位置和缩放处于初始状态 上面两行代码并不会造成控件重绘
        }

        public bool Contains(STNode node) {
            return this.IndexOf(node) != -1;
        }

        public int IndexOf(STNode node) {
            return Array.IndexOf<STNode>(m_nodes, node);
        }

        public void Insert(int nIndex, STNode node) {
            if (nIndex < 0 || nIndex >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            if (node == null)
                throw new ArgumentNullException("插入对象不能为空");
            this.EnsureSpace(1);
            for (int i = this._Count; i > nIndex; i--)
                m_nodes[i] = m_nodes[i - 1];
            node.Owner = m_owner;
            m_nodes[nIndex] = node;
            this._Count++;
            //node.BuildSize(true, true,false);
            m_owner.Invalidate();
            m_owner.BuildBounds();
        }

        public bool IsFixedSize {
            get { return false; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public void Remove(STNode node) {
            int nIndex = this.IndexOf(node);
            if (nIndex != -1) this.RemoveAt(nIndex);
        }

        public void RemoveAt(int nIndex) {
            if (nIndex < 0 || nIndex >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            m_nodes[nIndex].Owner = null;
            m_owner.InternalRemoveSelectedNode(m_nodes[nIndex]);
            if (m_owner.ActiveNode == m_nodes[nIndex]) m_owner.SetActiveNode(null);
            m_owner.OnNodeRemoved(new STNodeEditorEventArgs(m_nodes[nIndex]));
            this._Count--;
            for (int i = nIndex, Len = this._Count; i < Len; i++)
                m_nodes[i] = m_nodes[i + 1];
            if (this._Count == 0) {             //当不存在节点时候 坐标系回归
                m_owner.ScaleCanvas(1, 0, 0);
                m_owner.MoveCanvas(10, 10, true, CanvasMoveArgs.All);
            } else {
                m_owner.Invalidate();
                m_owner.BuildBounds();
            }
        }

        public STNode this[int nIndex] {
            get {
                if (nIndex < 0 || nIndex >= this._Count)
                    throw new IndexOutOfRangeException("索引越界");
                return m_nodes[nIndex];
            }
            set { throw new InvalidOperationException("禁止重新赋值元素"); }
        }

        public void CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException("数组不能为空");
            m_nodes.CopyTo(array, index);
        }

        public bool IsSynchronized {
            get { return true; }
        }

        public object SyncRoot {
            get { return this; }
        }

        public IEnumerator GetEnumerator() {
            for (int i = 0, Len = this._Count; i < Len; i++)
                yield return m_nodes[i];
        }
        /// <summary>
        /// 确认空间是否足够 空间不足扩大容量
        /// </summary>
        /// <param name="elements">需要增加的个数</param>
        private void EnsureSpace(int elements) {
            if (elements + this._Count > m_nodes.Length) {
                STNode[] arrTemp = new STNode[Math.Max(m_nodes.Length * 2, elements + this._Count)];
                m_nodes.CopyTo(arrTemp, 0);
                m_nodes = arrTemp;
            }
        }
        //============================================================================
        int IList.Add(object value) {
            return this.Add((STNode)value);
        }

        void IList.Clear() {
            this.Clear();
        }

        bool IList.Contains(object value) {
            return this.Contains((STNode)value);
        }

        int IList.IndexOf(object value) {
            return this.IndexOf((STNode)value);
        }

        void IList.Insert(int index, object value) {
            this.Insert(index, (STNode)value);
        }

        bool IList.IsFixedSize {
            get { return this.IsFixedSize; }
        }

        bool IList.IsReadOnly {
            get { return this.IsReadOnly; }
        }

        void IList.Remove(object value) {
            this.Remove((STNode)value);
        }

        void IList.RemoveAt(int index) {
            this.RemoveAt(index);
        }

        object IList.this[int index] {
            get {
                return this[index];
            }
            set {
                this[index] = (STNode)value;
            }
        }

        void ICollection.CopyTo(Array array, int index) {
            this.CopyTo(array, index);
        }

        int ICollection.Count {
            get { return this._Count; }
        }

        bool ICollection.IsSynchronized {
            get { return this.IsSynchronized; }
        }

        object ICollection.SyncRoot {
            get { return this.SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public STNode[] ToArray() {
            STNode[] nodes = new STNode[this._Count];
            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = m_nodes[i];
            return nodes;
        }
    }
}
