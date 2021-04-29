using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;

namespace ST.Library.UI.NodeEditor
{
    public class STNodeOptionCollection : IList, ICollection, IEnumerable
    {
        /*
         * 虽然该集合提供了完整的数据接口 如:Add,Remove,...
         * 但是尽可能的不要使用移除的一些操作 如:Remove,RemoveAt,Clear,this[index] = value,...
         * 因为在我的定义里面 每个Option的Owner是严格绑定的 一些移除或替换等操作会影响到Owner的变更
         * 所以原本的所有连线将会断开 并且触发DisConnect事件
         * 为了确保安全在STNode中 仅继承者才能够访问集合
         */
        private int _Count;
        public int Count { get { return _Count; } }
        private STNodeOption[] m_options;
        private STNode m_owner;

        private bool m_isInput;     //当前集合是否是存放的是输入点

        internal STNodeOptionCollection(STNode owner, bool isInput) {
            if (owner == null) throw new ArgumentNullException("所有者不能为空");
            m_owner = owner;
            m_isInput = isInput;
            m_options = new STNodeOption[4];
        }

        public STNodeOption Add(string strText, Type dataType, bool bSingle) {
            //not do this code -> out of bounds
            //return m_options[this.Add(new STNodeOption(strText, dataType, bSingle))];
            int nIndex = this.Add(new STNodeOption(strText, dataType, bSingle));
            return m_options[nIndex];
        }

        public int Add(STNodeOption option) {
            if (option == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(1);
            int nIndex = option == STNodeOption.Empty ? -1 : this.IndexOf(option);
            if (-1 == nIndex) {
                nIndex = this._Count;
                option.Owner = m_owner;
                option.IsInput = m_isInput;
                m_options[this._Count++] = option;
                this.Invalidate();
            }
            return nIndex;
        }

        public void AddRange(STNodeOption[] options) {
            if (options == null) throw new ArgumentNullException("添加对象不能为空");
            this.EnsureSpace(options.Length);
            foreach (var op in options) {
                if (op == null) throw new ArgumentNullException("添加对象不能为空");
                if (-1 == this.IndexOf(op)) {
                    op.Owner = m_owner;
                    op.IsInput = m_isInput;
                    m_options[this._Count++] = op;
                }
            }
            this.Invalidate();
        }

        public void Clear() {
            for (int i = 0; i < this._Count; i++) m_options[i].Owner = null;
            this._Count = 0;
            m_options = new STNodeOption[4];
            this.Invalidate();
        }

        public bool Contains(STNodeOption option) {
            return this.IndexOf(option) != -1;
        }

        public int IndexOf(STNodeOption option) {
            return Array.IndexOf<STNodeOption>(m_options, option);
        }

        public void Insert(int index, STNodeOption option) {
            if (index < 0 || index >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            if (option == null)
                throw new ArgumentNullException("插入对象不能为空");
            this.EnsureSpace(1);
            for (int i = this._Count; i > index; i--)
                m_options[i] = m_options[i - 1];
            option.Owner = m_owner;
            m_options[index] = option;
            this._Count++;
            this.Invalidate();
        }

        public bool IsFixedSize {
            get { return false; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public void Remove(STNodeOption option) {
            int nIndex = this.IndexOf(option);
            if (nIndex != -1) this.RemoveAt(nIndex);
        }

        public void RemoveAt(int index) {
            if (index < 0 || index >= this._Count)
                throw new IndexOutOfRangeException("索引越界");
            this._Count--;
            m_options[index].Owner = null;
            for (int i = index, Len = this._Count; i < Len; i++)
                m_options[i] = m_options[i + 1];
            this.Invalidate();
        }

        public STNodeOption this[int index] {
            get {
                if (index < 0 || index >= this._Count)
                    throw new IndexOutOfRangeException("索引越界");
                return m_options[index];
            }
            set { throw new InvalidOperationException("禁止重新赋值元素"); }
        }

        public void CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException("数组不能为空");
            m_options.CopyTo(array, index);
        }

        public bool IsSynchronized {
            get { return true; }
        }

        public object SyncRoot {
            get { return this; }
        }

        public IEnumerator GetEnumerator() {
            for (int i = 0, Len = this._Count; i < Len; i++)
                yield return m_options[i];
        }
        /// <summary>
        /// 确认空间是否足够 空间不足扩大容量
        /// </summary>
        /// <param name="elements">需要增加的个数</param>
        private void EnsureSpace(int elements) {
            if (elements + this._Count > m_options.Length) {
                STNodeOption[] arrTemp = new STNodeOption[Math.Max(m_options.Length * 2, elements + this._Count)];
                m_options.CopyTo(arrTemp, 0);
                m_options = arrTemp;
            }
        }

        protected void Invalidate() {
            if (m_owner != null && m_owner.Owner != null) {
                m_owner.BuildSize(true, true, true);
                //m_owner.Invalidate();//.Owner.Invalidate();
            }
        }
        //===================================================================================
        int IList.Add(object value) {
            return this.Add((STNodeOption)value);
        }

        void IList.Clear() {
            this.Clear();
        }

        bool IList.Contains(object value) {
            return this.Contains((STNodeOption)value);
        }

        int IList.IndexOf(object value) {
            return this.IndexOf((STNodeOption)value);
        }

        void IList.Insert(int index, object value) {
            this.Insert(index, (STNodeOption)value);
        }

        bool IList.IsFixedSize {
            get { return this.IsFixedSize; }
        }

        bool IList.IsReadOnly {
            get { return this.IsReadOnly; }
        }

        void IList.Remove(object value) {
            this.Remove((STNodeOption)value);
        }

        void IList.RemoveAt(int index) {
            this.RemoveAt(index);
        }

        object IList.this[int index] {
            get {
                return this[index];
            }
            set {
                this[index] = (STNodeOption)value;
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

        public STNodeOption[] ToArray() {
            STNodeOption[] ops = new STNodeOption[this._Count];
            for (int i = 0; i < ops.Length; i++)
                ops[i] = m_options[i];
            return ops;
        }
    }
}