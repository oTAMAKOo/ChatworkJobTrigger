
using System;
using System.Linq;

namespace Extensions
{
    /// <summary> 固定長のQueue </summary>
    public sealed class FixedQueue<T> : System.Collections.Generic.Queue<T>
    {
        //----- params -----

        public const int DefaultLength = 4096;

        //----- field -----

        private int length = 0;

        //----- property -----

        public int Length { get { return this.length; } }

        //----- method -----

        public FixedQueue()
        {
            this.length = DefaultLength;
        }

        public FixedQueue(int length)
        {
            this.length = length;
        }

        public new void Enqueue(T item)
        {
            if (length <= Count)
            {
                Dequeue();
            }

            base.Enqueue(item);
        }

        public void Remove(T item)
        {
            var items = this.Where(x => !x.Equals(item)).ToArray();

            Clear();

            foreach (var obj in items)
            {
                Enqueue(obj);
            }
        }
    }
}
