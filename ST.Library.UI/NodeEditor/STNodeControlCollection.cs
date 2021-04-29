using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ST.Library.UI.NodeEditor
{
    public class STNodeControlCollection: IList, ICollection, IEnumerable
    {
        /*
         * 为了确保安全在STNode中 仅继承者才能够访问集合
         */
        private int _Count;
        public int Count { get { return _Count; } }
        private STNodeControl[] m_controls;
        private STNode m_owner;

        internal STNodeControlCollection(STNode owner) {
            if (owner == null) throw new ArgumentNullException("所有者不能为空");
            m_owner = owner;
            m_controls = new STNodeControl[4];
        }

        public int Add(STNodeControl control) {
            if (control == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(1);
            int nIndex = this.IndexOf(control);
            if (-1 == nIndex) {
                nIndex = this._Count;
                control.Owner = m_owner;
                m_controls[this._Count++] = control;
                this.Redraw();
            }
            return nIndex;
        }

        public void AddRange(STNodeControl[] controls) {
            if (controls == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(controls.Length);
            foreach (var op in controls) {
                if (op == null) throw new ArgumentNullException("添加对象不能为空");
                if (-1 == this.IndexOf(op)) {
                    op.Owner = m_owner;
                    m_controls[this._Count++] = op;
                }
            }
            this.Redraw();
        }

        public void Clear() {
            for (int i = 0; i < this._Count; i++) m_controls[i].Owner = null;
            this._Count = 0;
            m_controls = new STNodeControl[4];
            this.Redraw();
        }

        public bool Contains(STNodeControl option) {
            return this.IndexOf(option) != -1;
        }

        public int IndexOf(STNodeControl option) {
            return Array.IndexOf<STNodeControl>(m_controls, option);
        }

        public void Insert(int index, STNodeControl control) {
            if (index < 0 || index >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            if (control == null)
                throw new ArgumentNullException("插入对象不能为空");
            this.EnsureSpace(1);
            for (int i = this._Count; i > index; i--)
                m_controls[i] = m_controls[i - 1];
            control.Owner = m_owner;
            m_controls[index] = control;
            this._Count++;
            this.Redraw();
        }

        public bool IsFixedSize {
            get { return false; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public void Remove(STNodeControl control) {
            int nIndex = this.IndexOf(control);
            if (nIndex != -1) this.RemoveAt(nIndex);
        }

        public void RemoveAt(int index) {
            if (index < 0 || index >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            this._Count--;
            m_controls[index].Owner = null;
            for (int i = index, Len = this._Count; i < Len; i++)
                m_controls[i] = m_controls[i + 1];
            this.Redraw();
        }

        public STNodeControl this[int index] {
            get {
                if (index < 0 || index >= this._Count)
                    throw new IndexOutOfRangeException("索引越界");
                return m_controls[index];
            }
            set { throw new InvalidOperationException("禁止重新赋值元素"); }
        }

        public void CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException("数组不能为空");
            m_controls.CopyTo(array, index);
        }

        public bool IsSynchronized {
            get { return true; }
        }

        public object SyncRoot {
            get { return this; }
        }

        public IEnumerator GetEnumerator() {
            for (int i = 0, Len = this._Count; i < Len; i++)
                yield return m_controls[i];
        }
        /// <summary>
        /// 确认空间是否足够 空间不足扩大容量
        /// </summary>
        /// <param name="elements">需要增加的个数</param>
        private void EnsureSpace(int elements) {
            if (elements + this._Count > m_controls.Length) {
                STNodeControl[] arrTemp = new STNodeControl[Math.Max(m_controls.Length * 2, elements + this._Count)];
                m_controls.CopyTo(arrTemp, 0);
                m_controls = arrTemp;
            }
        }

        protected void Redraw() {
            if (m_owner != null && m_owner.Owner != null) {
                //m_owner.BuildSize();
                m_owner.Owner.Invalidate(m_owner.Owner.CanvasToControl(m_owner.Rectangle));
            }
        }
        //===================================================================================
        int IList.Add(object value) {
            return this.Add((STNodeControl)value);
        }

        void IList.Clear() {
            this.Clear();
        }

        bool IList.Contains(object value) {
            return this.Contains((STNodeControl)value);
        }

        int IList.IndexOf(object value) {
            return this.IndexOf((STNodeControl)value);
        }

        void IList.Insert(int index, object value) {
            this.Insert(index, (STNodeControl)value);
        }

        bool IList.IsFixedSize {
            get { return this.IsFixedSize; }
        }

        bool IList.IsReadOnly {
            get { return this.IsReadOnly; }
        }

        void IList.Remove(object value) {
            this.Remove((STNodeControl)value);
        }

        void IList.RemoveAt(int index) {
            this.RemoveAt(index);
        }

        object IList.this[int index] {
            get {
                return this[index];
            }
            set {
                this[index] = (STNodeControl)value;
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
    }
}
